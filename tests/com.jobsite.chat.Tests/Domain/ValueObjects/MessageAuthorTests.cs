using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Domain.ValueObjects;

// Behaviors 16–17: MessageAuthor.User / MessageAuthor.Bot.
public class MessageAuthorTests
{
    // Behavior 16
    [Fact]
    public void User_ValidInput_IsBotFalseAndFieldsSet()
    {
        MessageAuthor author = MessageAuthor.User("id", "Ana");
        Assert.False(author.IsBot);
        Assert.Equal("id", author.UserId);
        Assert.Equal("Ana", author.DisplayName);
    }

    // Behavior 16
    [Fact]
    public void User_TrimsDisplayName()
    {
        MessageAuthor author = MessageAuthor.User("id", "  Ana  ");
        Assert.Equal("Ana", author.DisplayName);
    }

    // Behavior 16
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void User_NullOrWhitespaceUserId_ThrowsDomainException(string? userId)
    {
        Assert.Throws<DomainException>(() => MessageAuthor.User(userId!, "Ana"));
    }

    // Behavior 16
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void User_NullOrWhitespaceDisplayName_ThrowsDomainException(string? displayName)
    {
        Assert.Throws<DomainException>(() => MessageAuthor.User("id", displayName!));
    }

    // Behavior 16
    [Fact]
    public void User_DisplayNameLongerThan100_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => MessageAuthor.User("id", new string('a', 101)));
    }

    // Behavior 16
    [Fact]
    public void User_DisplayNameExactly100_Succeeds()
    {
        string name = new string('a', 100);
        MessageAuthor author = MessageAuthor.User("id", name);
        Assert.Equal(name, author.DisplayName);
    }

    // Behavior 17
    [Fact]
    public void Bot_Default_HasNullUserIdAndDefaultName()
    {
        MessageAuthor author = MessageAuthor.Bot();
        Assert.Null(author.UserId);
        Assert.True(author.IsBot);
        Assert.Equal("StockBot", author.DisplayName);
    }

    // Behavior 17
    [Fact]
    public void Bot_GivenName_UsesGivenName()
    {
        MessageAuthor author = MessageAuthor.Bot("Quoter");
        Assert.True(author.IsBot);
        Assert.Equal("Quoter", author.DisplayName);
    }

    // Behavior 17
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bot_WhitespaceName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => MessageAuthor.Bot(name));
    }

    // Behavior 17
    [Fact]
    public void Bot_NameLongerThan100_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => MessageAuthor.Bot(new string('a', 101)));
    }
}
