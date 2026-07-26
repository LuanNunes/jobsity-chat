using System.Security.Claims;
using com.jobsite.chat.Api.Identity;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace com.jobsite.chat.Api.Endpoints;

// JSON auth surface consumed by the SPA (spec §2.3). Hand-rolled (not AddIdentityApiEndpoints) so
// the DisplayName-as-claim contract and the exact AuthUserDto shape stay under our control.
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", Me).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterDto request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        ApplicationUser user = new()
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);

        return result.Succeeded
            ? await SignInNewUserAsync(user, userManager, signInManager)
            : Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["identity"] = (from error in result.Errors select error.Description).ToArray(),
            });
    }

    private static async Task<IResult> SignInNewUserAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        await userManager.AddClaimAsync(user, new Claim(AppClaimTypes.DisplayName, user.DisplayName));
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Ok(AuthUserDto.FromEntity(user));
    }

    private static async Task<IResult> LoginAsync(
        LoginDto request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);

        SignInResult result = user is null
            ? SignInResult.Failed
            : await user.SignInWithPasswordAsync(signInManager, request.Password, request.RememberMe);

        return user is null || !result.Succeeded
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials.")
            : Results.Ok(AuthUserDto.FromEntity(user));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static IResult Me(ClaimsPrincipal principal)
    {
        string id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        string email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        string displayName = principal.FindFirstValue(AppClaimTypes.DisplayName) ?? string.Empty;

        return Results.Ok(new AuthUserDto(id, email, displayName));
    }
}
