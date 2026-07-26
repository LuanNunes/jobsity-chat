using System.Threading.RateLimiting;
using com.jobsite.chat.Api.Features.Auth;
using com.jobsite.chat.Api.Features.Rooms;
using com.jobsite.chat.Api.Features.Chat;
using com.jobsite.chat.Api.Infrastructure;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Api.Exceptions;
using com.jobsite.chat.Repository;
using com.jobsite.chat.Repository.Persistence.Context;
using com.jobsite.chat.Service;
using com.jobsite.chat.Shared.Logging;
using com.jobsite.chat.Shared.Messaging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Serilog;

// Public so WebApplicationFactory<Program> can host it in integration tests.
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Bootstrap logger: captures startup failures before the host (and its configured sinks) exist.
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            WebApplication app = BuildApplication(args);
            await app.MigrateDatabasesAsync();
            ConfigurePipeline(app);
            await app.RunAsync();
            await Log.CloseAndFlushAsync();
            return 0;
        }
        catch (Exception exception) when (IsHostStopSignal(exception))
        {
            // WebApplicationFactory<Program> intercepts host startup by throwing an internal
            // control-flow exception (StopTheHostException) out of RunAsync. It must propagate so the
            // factory receives the built host; we must NOT flush/dispose the process-global static
            // Serilog logger here, or a parallel test host would be torn down mid-run.
            throw;
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Api host terminated unexpectedly.");
            await Log.CloseAndFlushAsync();
            return 1;
        }
    }

    private static bool IsHostStopSignal(Exception exception) =>
        exception is HostAbortedException || exception.GetType().Name == "StopTheHostException";

    private static WebApplication BuildApplication(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Format chosen in code by environment: readable console in Development, compact JSON otherwise.
        // Only MinimumLevel/Override/Enrich come from the Serilog config section (no WriteTo in config).
        // preserveStaticLogger: true keeps the per-host logger out of the process-global Log.Logger so
        // parallel in-process test hosts (WebApplicationFactory) don't race to freeze the shared
        // bootstrap logger ("The logger is already frozen.").
        builder.Services.AddSerilog(
            (services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .ConfigureConsoleSink(builder.Environment.IsDevelopment()),
            preserveStaticLogger: true);

        builder.Services.AddServiceLayer();
        builder.Services.AddRepositoryLayer(builder.Configuration);

        // Identity for an API (no server-rendered UI): Core + SignInManager + the application cookie scheme.
        // AddIdentityCookies wires PasswordSignInAsync/SignInAsync/SignOutAsync to the Identity application
        // cookie and sets it as the default scheme so [Authorize] and the hub challenge the cookie.
        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireNonAlphanumeric = false;   // dev relaxation
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;             // dev: SPA and API are same-site (localhost)
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;  // dev: cookie set/sent over http
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            // API surface only: unauthenticated -> 401 JSON, forbidden -> 403 JSON; never a 302 to a login page.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        string spaOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:3000";
        builder.Services.AddCors(options =>
            options.AddPolicy("Spa", policy =>
                policy.WithOrigins(spaOrigin)   // explicit origin — AllowAnyOrigin is illegal with credentials
                      .AllowAnyHeader()
                      .AllowAnyMethod()          // GET/POST/preflight; SignalR needs GET+POST
                      .AllowCredentials()));     // browser sends/accepts the cookie (and for SignalR)

        AddRateLimiting(builder);

        builder.Services.AddSignalR(options => options.AddFilter<HubExceptionFilter>());

        // RabbitMQ: publish stock-quote requests + consume replies to persist/broadcast bot messages.
        builder.Services.AddRabbitMqCore(builder.Configuration);
        builder.Services.AddStockRequestPublisher();
        builder.Services.AddHostedService<StockReplyConsumer>();

        // Health: liveness = no probes; readiness = SQLite (EF) + RabbitMQ, tagged "ready".
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ChatDbContext>("database", tags: ["ready"])
            .AddRabbitMqHealthCheck(tags: ["ready"]);

        return builder.Build();
    }

    private static void AddRateLimiting(WebApplicationBuilder builder)
    {
        // Bound (not read eagerly): WebApplicationFactory applies its config overrides AFTER
        // BuildApplication runs, so the limits must be resolved per-request from DI, not at build time.
        builder.Services.AddOptions<RateLimitingOptions>()
            .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global per-IP fixed window, but health probes and SignalR negotiation are never throttled.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (context.Request.Path.StartsWithSegments("/health")
                    || context.Request.Path.StartsWithSegments("/hubs"))
                {
                    return RateLimitPartition.GetNoLimiter("exempt");
                }

                int permitLimit = ResolveLimits(context).PermitPerMinute;
                string clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                });
            });

            // Tight per-IP limiter for credential endpoints (login + register).
            options.AddPolicy("auth", context =>
            {
                int permitLimit = ResolveLimits(context).AuthPermitPerMinute;
                string clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter($"auth:{clientKey}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                });
            });
        });
    }

    private static RateLimitingOptions ResolveLimits(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseRouting();
        app.UseCors("Spa");        // BEFORE auth and BEFORE MapHub — SignalR requires CORS ahead of the hub
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapAuthEndpoints();
        app.MapRoomEndpoints();
        app.MapHub<ChatHub>("/hubs/chat");

        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false }); // liveness
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
        });
    }
}
