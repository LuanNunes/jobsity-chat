using Microsoft.AspNetCore.Identity;

namespace com.jobsite.chat.Domain.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; init; } = string.Empty;

    public static ApplicationUser Create(string email, string displayName) =>
        new()
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
        };

    public Task<SignInResult> SignInWithPasswordAsync(
        SignInManager<ApplicationUser> signInManager, string password, bool rememberMe) =>
        signInManager.PasswordSignInAsync(this, password, rememberMe, lockoutOnFailure: false);
}
