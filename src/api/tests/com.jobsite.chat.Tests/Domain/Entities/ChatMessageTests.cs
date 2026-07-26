using com.jobsite.chat.Domain.Dtos;
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

        ChatMessage message = ChatMessage.Create(new NewChatMessageDto(roomId, author, "  hello  ", SentAt));

        Assert.Equal(roomId, message.RoomId);
        Assert.Same(author, message.Author);
        Assert.Equal("hello", message.Content);
        Assert.Equal(SentAt, message.SentAtUtc);
    }

    // Behavior 14
    [Fact]
    public void Create_ValidInput_GeneratesNonEmptyId()
    {
        ChatMessage message = ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt));
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    // Behavior 14
    [Fact]
    public void Create_TwoMessages_GeneratesUniqueIds()
    {
        ChatMessage a = ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt));
        ChatMessage b = ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), "hi", SentAt));
        Assert.NotEqual(a.Id, b.Id);
    }

    // Behavior 15
    [Fact]
    public void Create_EmptyRoomId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.Empty, MessageAuthor.User("u", "Ana"), "hi", SentAt)));
    }

    // Behavior 15
    [Fact]
    public void Create_NullAuthor_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), null!, "hi", SentAt)));
    }

    // Behavior 15
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhitespaceContent_ThrowsDomainException(string? content)
    {
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content!, SentAt)));
    }

    // Behavior 15
    [Fact]
    public void Create_ContentLongerThan1000AfterTrim_ThrowsDomainException()
    {
        string content = new string('x', 1001);
        Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content, SentAt)));
    }

    // Behavior 15
    [Fact]
    public void Create_ContentExactly1000_Succeeds()
    {
        string content = new string('x', 1000);
        ChatMessage message = ChatMessage.Create(new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content, SentAt));
        Assert.Equal(content, message.Content);
    }

    // rev. 3 spec §4.1 — aggregation: all invalid fields reported in one message.
    [Fact]
    public void Create_AllFieldsInvalid_ThrowsSingleDomainExceptionListingEveryViolation()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            ChatMessage.Create(new NewChatMessageDto(Guid.Empty, null!, "", DateTimeOffset.UtcNow)));

        Assert.Contains("'Room Id'", exception.Message);
        Assert.Contains("'Author'", exception.Message);
        Assert.Contains("'Content'", exception.Message);
    }

    // rev. 3 spec §4.5 — trim precedes validation: padded 1000-char content passes and stores trimmed.
    [Fact]
    public void Create_PaddedContentExactly1000AfterTrim_SucceedsWithLength1000()
    {
        string content = "  " + new string('a', 1000) + "  ";
        ChatMessage message = ChatMessage.Create(
            new NewChatMessageDto(Guid.NewGuid(), MessageAuthor.User("u", "Ana"), content, SentAt));
        Assert.Equal(1000, message.Content.Length);
    }

    // rev. 3.1 spec §4.6 — NewChatMessage supports `with` derivation used by future call sites.
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
