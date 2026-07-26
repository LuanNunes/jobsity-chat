using Microsoft.AspNetCore.Identity;

namespace com.jobsite.chat.Domain.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; init; } = string.Empty;

    // Signs this user in with their password via the Identity cookie scheme. Uses the user-instance
    // overload of PasswordSignInAsync, so the caller must resolve the user (e.g. by email) first.
    public Task<SignInResult> SignInWithPasswordAsync(
        SignInManager<ApplicationUser> signInManager, string password, bool rememberMe) =>
        signInManager.PasswordSignInAsync(this, password, rememberMe, lockoutOnFailure: false);
}
