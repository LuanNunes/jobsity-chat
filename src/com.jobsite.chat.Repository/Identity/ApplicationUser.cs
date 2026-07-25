using Microsoft.AspNetCore.Identity;

namespace com.jobsite.chat.Repository.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
