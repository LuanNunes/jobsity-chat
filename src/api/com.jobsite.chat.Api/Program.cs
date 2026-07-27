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

public class Program
{
    public static async Task<int> Main(string[] args)
    {

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

        builder.Services.AddSerilog(
            (services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .ConfigureConsoleSink(builder.Environment.IsDevelopment()),
            preserveStaticLogger: true);

        builder.Services.AddServiceLayer();
        builder.Services.AddRepositoryLayer(builder.Configuration);

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireNonAlphanumeric = false;
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
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.None
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

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
                policy.WithOrigins(spaOrigin)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()));

        AddRateLimiting(builder);

        builder.Services.AddSignalR(options => options.AddFilter<HubExceptionFilter>());

        builder.Services.AddRabbitMqCore(builder.Configuration);
        builder.Services.AddStockRequestPublisher();
        builder.Services.AddHostedService<StockReplyConsumer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ChatDbContext>("database", tags: ["ready"])
            .AddRabbitMqHealthCheck(tags: ["ready"]);

        return builder.Build();
    }

    private static void AddRateLimiting(WebApplicationBuilder builder)
    {

        builder.Services.AddOptions<RateLimitingOptions>()
            .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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
        app.UseCors("Spa");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapAuthEndpoints();
        app.MapRoomEndpoints();
        app.MapHub<ChatHub>("/hubs/chat");

        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
        });
    }
}
