using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Domain.ValueObjects;

public class MessageAuthorTests
{

    [Fact]
    public void User_ValidInput_IsBotFalseAndFieldsSet()
    {
        MessageAuthor author = MessageAuthor.User("id", "Ana");
        Assert.False(author.IsBot);
        Assert.Equal("id", author.UserId);
        Assert.Equal("Ana", author.DisplayName);
    }

    [Fact]
    public void User_TrimsDisplayName()
    {
        MessageAuthor author = MessageAuthor.User("id", "  Ana  ");
        Assert.Equal("Ana", author.DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void User_NullOrWhitespaceUserId_ThrowsDomainException(string? userId)
    {
        Assert.Throws<DomainException>(() => MessageAuthor.User(userId!, "Ana"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void User_NullOrWhitespaceDisplayName_ThrowsDomainException(string? displayName)
    {
        Assert.Throws<DomainException>(() => MessageAuthor.User("id", displayName!));
    }

    [Fact]
    public void User_DisplayNameLongerThan100_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => MessageAuthor.User("id", new string('a', 101)));
    }

    [Fact]
    public void User_DisplayNameExactly100_Succeeds()
    {
        string name = new string('a', 100);
        MessageAuthor author = MessageAuthor.User("id", name);
        Assert.Equal(name, author.DisplayName);
    }

    [Fact]
    public void Bot_Default_HasNullUserIdAndDefaultName()
    {
        MessageAuthor author = MessageAuthor.Bot();
        Assert.Null(author.UserId);
        Assert.True(author.IsBot);
        Assert.Equal("StockBot", author.DisplayName);
    }

    [Fact]
    public void Bot_GivenName_UsesGivenName()
    {
        MessageAuthor author = MessageAuthor.Bot("Quoter");
        Assert.True(author.IsBot);
        Assert.Equal("Quoter", author.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bot_WhitespaceName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => MessageAuthor.Bot(name));
    }

    [Fact]
    public void Bot_NameLongerThan100_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => MessageAuthor.Bot(new string('a', 101)));
    }

    [Fact]
    public void User_UserIdAndDisplayNameEmpty_ThrowsSingleDomainExceptionListingBothViolations()
    {
        DomainException exception = Assert.Throws<DomainException>(() => MessageAuthor.User("", ""));

        Assert.Contains("'User Id'", exception.Message);
        Assert.Contains("'Display Name'", exception.Message);
    }

    [Fact]
    public void User_NullUserId_ThrowsDomainExceptionAndDoesNotBecomeBot()
    {
        Assert.Throws<DomainException>(() => MessageAuthor.User(null!, "Ana"));
    }
}
