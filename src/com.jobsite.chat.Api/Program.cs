using com.jobsite.chat.Api.Features.Auth;
using com.jobsite.chat.Api.Features.Rooms;
using com.jobsite.chat.Api.Features.Chat;
using com.jobsite.chat.Api.Infrastructure;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Api.Exceptions;
using com.jobsite.chat.Repository;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using com.jobsite.chat.Service;
using com.jobsite.chat.Shared.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

// Public so WebApplicationFactory<Program> can host it in integration tests.
public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplication app = BuildApplication(args);
        await app.MigrateDatabasesAsync();
        ConfigurePipeline(app);
        await app.RunAsync();
    }

    private static WebApplication BuildApplication(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

        builder.Services.AddSignalR(options => options.AddFilter<HubExceptionFilter>());

        // RabbitMQ: publish stock-quote requests + consume replies to persist/broadcast bot messages.
        builder.Services.AddRabbitMqCore(builder.Configuration);
        builder.Services.AddStockRequestPublisher();
        builder.Services.AddHostedService<StockReplyConsumer>();

        return builder.Build();
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseRouting();
        app.UseCors("Spa");        // BEFORE auth and BEFORE MapHub — SignalR requires CORS ahead of the hub
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapAuthEndpoints();
        app.MapRoomEndpoints();
        app.MapHub<ChatHub>("/hubs/chat");
    }
}
