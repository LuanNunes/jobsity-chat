namespace com.jobsite.chat.Api.Infrastructure.Identity;

// Non-default claim carrying the user's chat display name; persisted at registration.
public static class AppClaimTypes
{
    public const string DisplayName = "display_name";
}
