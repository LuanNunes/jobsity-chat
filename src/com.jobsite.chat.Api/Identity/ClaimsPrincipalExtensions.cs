using System.Security.Claims;

namespace com.jobsite.chat.Api.Identity;

// The ONLY trusted source of chat sender identity: derives (userId, displayName) from the cookie principal.
public static class ClaimsPrincipalExtensions
{
    public static (string UserId, string DisplayName) ToChatAuthor(this ClaimsPrincipal principal)
    {
        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? displayName = principal.FindFirstValue(AppClaimTypes.DisplayName);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Authenticated principal is missing user id or display name.");
        }

        return (userId, displayName);
    }
}
