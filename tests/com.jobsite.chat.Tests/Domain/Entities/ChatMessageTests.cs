using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Domain.Entities;

// Behaviors 14–15: ChatMessage.Create.
public class ChatMessageTests
{
    private static readonly DateTimeOffset SentAt = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    // Behavior 14
    [Fact]
    public void Create_ValidInput_SetsAllFieldsAndTrimsContent()
    {
        Guid roomId = Guid.NewGuid();
        MessageAuthor author = MessageAuthor.User("user-1", "Ana");

        ChatMessage message = ChatMessage.Create(roomId, author, "  hello  ", SentAt);

        Assert.Equal(roomId, message.RoomId);
        Assert.Same(author, message.Author);
        Assert.Equal("hello", message.Content);
        Assert.Equal(SentAt, message.SentAtUtc);
    }

    // Behavior 14
    [Fact]
    public void Create_ValidInput_GeneratesNonEmptyId()
    {
        ChatMessage message = ChatMessage.Create(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt);
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    // Behavior 14
    [Fact]
    public void Create_TwoMessages_GeneratesUniqueIds()
    {
        ChatMessage a = ChatMessage.Create(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt);
        ChatMessage b = ChatMessage.Create(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt);
        Assert.NotEqual(a.Id, b.Id);
    }

    // Behavior 15
    [Fact]
    public void Create_EmptyRoomId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(Guid.Empty, MessageAuthor.User("u", "Ana"), "hi", SentAt));
    }

    // Behavior 15
    [Fact]
    public void Create_NullAuthor_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(Guid.NewGuid(), null!, "hi", SentAt));
    }

    // Behavior 15
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhitespaceContent_ThrowsDomainException(string? content)
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content!, SentAt));
    }

    // Behavior 15
    [Fact]
    public void Create_ContentLongerThan1000AfterTrim_ThrowsDomainException()
    {
        string content = new string('x', 1001);
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content, SentAt));
    }

    // Behavior 15
    [Fact]
    public void Create_ContentExactly1000_Succeeds()
    {
        string content = new string('x', 1000);
        ChatMessage message = ChatMessage.Create(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content, SentAt);
        Assert.Equal(content, message.Content);
    }
}
