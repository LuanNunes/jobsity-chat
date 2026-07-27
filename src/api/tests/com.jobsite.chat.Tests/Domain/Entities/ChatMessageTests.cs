using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Domain.Entities;

public class ChatMessageTests
{
    private static readonly DateTimeOffset SentAt = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidInput_SetsAllFieldsAndTrimsContent()
    {
        Guid roomId = Guid.NewGuid();
        MessageAuthor author = MessageAuthor.User("user-1", "Ana");

        ChatMessage message = ChatMessage.Create(new NewChatMessageDto(roomId, author, "  hello  ", SentAt));

        Assert.Equal(roomId, message.RoomId);
        Assert.Same(author, message.Author);
        Assert.Equal("hello", message.Content);
        Assert.Equal(SentAt, message.SentAtUtc);
    }

    [Fact]
    public void Create_ValidInput_GeneratesNonEmptyId()
    {
        ChatMessage message = ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt));
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void Create_TwoMessages_GeneratesUniqueIds()
    {
        ChatMessage a = ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt));
        ChatMessage b = ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt));
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Create_EmptyRoomId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.Empty, MessageAuthor.User("u", "Ana"), "hi", SentAt)));
    }

    [Fact]
    public void Create_NullAuthor_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), null!, "hi", SentAt)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhitespaceContent_ThrowsDomainException(string? content)
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content!, SentAt)));
    }

    [Fact]
    public void Create_ContentLongerThan1000AfterTrim_ThrowsDomainException()
    {
        string content = new string('x', 1001);
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content, SentAt)));
    }

    [Fact]
    public void Create_ContentExactly1000_Succeeds()
    {
        string content = new string('x', 1000);
        ChatMessage message = ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content, SentAt));
        Assert.Equal(content, message.Content);
    }

    [Fact]
    public void Create_AllFieldsInvalid_ThrowsSingleDomainExceptionListingEveryViolation()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.Empty, null!, "", DateTimeOffset.UtcNow)));

        Assert.Contains("'Room Id'", exception.Message);
        Assert.Contains("'Author'", exception.Message);
        Assert.Contains("'Content'", exception.Message);
    }

    [Fact]
    public void Create_PaddedContentExactly1000AfterTrim_SucceedsWithLength1000()
    {
        string content = "  " + new string('a', 1000) + "  ";
        ChatMessage message = ChatMessage.Create(
            new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content, SentAt));
        Assert.Equal(1000, message.Content.Length);
    }

    [Fact]
    public void Create_DraftDerivedWithExpression_SucceedsWithOverriddenContent()
    {
        NewChatMessageDto baseDraft = new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "base", SentAt);
        NewChatMessageDto derived = baseDraft with { Content = "derived" };

        ChatMessage message = ChatMessage.Create(derived);

        Assert.Equal("derived", message.Content);
        Assert.Equal(baseDraft.RoomId, message.RoomId);
    }
}
