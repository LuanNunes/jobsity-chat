using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Domain.Dtos;

public sealed record AuthUserDto(string Id, string Email, string DisplayName)
{
    public static AuthUserDto FromEntity(ApplicationUser user) =>
        new(user.Id, user.Email!, user.DisplayName);
}
