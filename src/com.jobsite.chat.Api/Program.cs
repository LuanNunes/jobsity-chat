using com.jobsite.chat.Api.Endpoints;
using com.jobsite.chat.Api.Hubs;
using com.jobsite.chat.Api.Infrastructure;
using com.jobsite.chat.Domain.Identity;
using com.jobsite.chat.Repository;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Service;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

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

// SEAM: no-op until the real RabbitMQ publisher lands in Shared (one-line DI swap here).
builder.Services.AddScoped<IStockQuoteRequestPublisher, NoOpStockQuoteRequestPublisher>();

WebApplication app = builder.Build();

await app.MigrateDatabasesAsync();

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

app.Run();

public partial class Program;   // exposes the composition root to WebApplicationFactory<Program>
