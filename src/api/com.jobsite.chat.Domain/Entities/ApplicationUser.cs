using Microsoft.AspNetCore.Identity;

namespace com.jobsite.chat.Domain.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; init; } = string.Empty;

    // Factory for registration. UserName mirrors Email (this app logs in by email). No domain
    // validation here: email/password rules are owned by ASP.NET Identity (UserManager.CreateAsync),
    // which returns an IdentityResult the endpoint surfaces as a 400 — not a thrown exception.
    public static ApplicationUser Create(string email, string displayName) =>
        new()
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
        };

    // Signs this user in with their password via the Identity cookie scheme. Uses the user-instance
    // overload of PasswordSignInAsync, so the caller must resolve the user (e.g. by email) first.
    public Task<SignInResult> SignInWithPasswordAsync(
        SignInManager<ApplicationUser> signInManager, string password, bool rememberMe) =>
        signInManager.PasswordSignInAsync(this, password, rememberMe, lockoutOnFailure: false);
}
