using System.Security.Claims;
using com.jobsite.chat.Api.Infrastructure.Identity;

namespace com.jobsite.chat.Tests.Api.Infrastructure.Identity;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
        => new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

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

    [Fact]
    public void ToChatAuthor_NameIdentifierMissing_ThrowsInvalidOperationException()
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(AppClaimTypes.DisplayName, "Ana"));

        Assert.Throws<InvalidOperationException>(() => principal.ToChatAuthor());
    }

    [Fact]
    public void ToChatAuthor_DisplayNameMissing_ThrowsInvalidOperationException()
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(ClaimTypes.NameIdentifier, "user-1"));

        Assert.Throws<InvalidOperationException>(() => principal.ToChatAuthor());
    }

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

    [Fact]
    public void ToChatAuthor_OnlyNameClaimNoNameIdentifier_ThrowsInvalidOperationException()
    {
        ClaimsPrincipal principal = PrincipalWith(
            new Claim(ClaimTypes.Name, "the-name-claim"),
            new Claim(AppClaimTypes.DisplayName, "Ana"));

        Assert.Throws<InvalidOperationException>(() => principal.ToChatAuthor());
    }
}
