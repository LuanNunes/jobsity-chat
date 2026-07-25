using com.jobsite.chat.Domain.Validation;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Domain.Entities;

// Core chat message entity; private ctor + static factory. EF Core materializes via
// the private ctor, bypassing the factory guards.
public sealed class ChatMessage
{
    public const int MaxContentLength = 1000;

    private ChatMessage(Guid id, Guid roomId, MessageAuthor author, string content, DateTimeOffset sentAtUtc)
    {
        Id = id;
        RoomId = roomId;
        Author = author;
        Content = content;
        SentAtUtc = sentAtUtc;
    }

    public Guid Id { get; }
    public Guid RoomId { get; }
    public MessageAuthor Author { get; }  // EF maps as owned/complex type later
    public string Content { get; }        // trimmed
    public DateTimeOffset SentAtUtc { get; }

    public static ChatMessage Create(Guid roomId, MessageAuthor author, string content, DateTimeOffset sentAtUtc)
    {
        Guid validRoomId = Ensure.NotEmpty(roomId, nameof(roomId));
        MessageAuthor validAuthor = Ensure.NotNull(author, nameof(author));
        string validContent = Ensure.MaxLength(
            Ensure.NotNullOrWhiteSpace(content, nameof(content)),
            MaxContentLength,
            nameof(content));
        return new ChatMessage(Guid.CreateVersion7(), validRoomId, validAuthor, validContent, sentAtUtc);
    }
}
