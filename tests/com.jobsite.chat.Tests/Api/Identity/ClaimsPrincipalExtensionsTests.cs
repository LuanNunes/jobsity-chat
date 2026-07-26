using System.Security.Claims;
using com.jobsite.chat.Api.Identity;

namespace com.jobsite.chat.Tests.Api.Identity;

// Spec §2.4 / §4.1: ToChatAuthor is the ONLY trusted source of sender identity.
public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
        => new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

    // returns (userId, displayName) when both claims present.
    [Fact]
    public void ToChatAuthor_BothClaimsPresent_ReturnsUserIdAndDisplayName()
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(AppClaimTypes.DisplayName, "Ana"));

        (string userId, string displayName) = principal.ToChatAuthor();

        Assert.Equal("user-1", userId);
        Assert.Equal("Ana", displayName);
    }

    // throws InvalidOperationException when NameIdentifier missing.
    [Fact]
    public void ToChatAuthor_NameIdentifierMissing_ThrowsInvalidOperationException()
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(AppClaimTypes.DisplayName, "Ana"));

        Assert.Throws<InvalidOperationException>(() => principal.ToChatAuthor());
    }

    // throws when DisplayName claim missing.
    [Fact]
    public void ToChatAuthor_DisplayNameMissing_ThrowsInvalidOperationException()
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(ClaimTypes.NameIdentifier, "user-1"));

        Assert.Throws<InvalidOperationException>(() => principal.ToChatAuthor());
    }

    // throws when DisplayName claim is whitespace.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ToChatAuthor_DisplayNameWhitespace_ThrowsInvalidOperationException(string displayName)
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(AppClaimTypes.DisplayName, displayName));

        Assert.Throws<InvalidOperationException>(() => principal.ToChatAuthor());
    }

    // throws when userId claim is whitespace.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ToChatAuthor_UserIdWhitespace_ThrowsInvalidOperationException(string userId)
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(AppClaimTypes.DisplayName, "Ana"));

        Assert.Throws<InvalidOperationException>(() => principal.ToChatAuthor());
    }

    // reads ClaimTypes.NameIdentifier specifically, NOT ClaimTypes.Name.
    [Fact]
    public void ToChatAuthor_UsesNameIdentifierNotName()
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(ClaimTypes.Name, "the-name-claim"),
            new Claim(ClaimTypes.NameIdentifier, "the-id-claim"),
            new Claim(AppClaimTypes.DisplayName, "Ana"));

        (string userId, string displayName) = principal.ToChatAuthor();

        Assert.Equal("the-id-claim", userId);
    }

    // when only ClaimTypes.Name (no NameIdentifier) is present, still throws (does not fall back to Name).
    [Fact]
    public void ToChatAuthor_OnlyNameClaimNoNameIdentifier_ThrowsInvalidOperationException()
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(ClaimTypes.Name, "the-name-claim"),
            new Claim(AppClaimTypes.DisplayName, "Ana"));

        Assert.Throws<InvalidOperationException>(() => principal.ToChatAuthor());
    }
}
