using com.jobsite.chat.Domain.Identity;

namespace com.jobsite.chat.Domain.Dtos;

// Authenticated-user output the SPA depends on for register/login/me. Built from an ApplicationUser or
// the cookie ClaimsPrincipal — never leaks the password hash or the full IdentityUser.
public sealed record AuthUserDto(string Id, string Email, string DisplayName)
{
    public static AuthUserDto FromEntity(ApplicationUser user) =>
        new(user.Id, user.Email!, user.DisplayName);
}
