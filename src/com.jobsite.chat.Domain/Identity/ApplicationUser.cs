using Microsoft.AspNetCore.Identity;

namespace com.jobsite.chat.Domain.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; init; } = string.Empty;
}
