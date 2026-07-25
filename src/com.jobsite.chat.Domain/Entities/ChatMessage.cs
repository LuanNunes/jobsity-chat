using FluentValidation;
using com.jobsite.chat.Domain.Dtos;
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

    private static readonly InlineValidator<ChatMessage> Rules = new()
    {
        v => v.RuleFor(m => m.RoomId).NotEmpty(),
        v => v.RuleFor(m => m.Author).NotNull(),
        v => v.RuleFor(m => m.Content).NotEmpty().MaximumLength(MaxContentLength),
    };

    public static ChatMessage Create(NewChatMessage draft)
    {
        ChatMessage message = new(
            Guid.CreateVersion7(), draft.RoomId, draft.Author!, draft.Content?.Trim() ?? string.Empty, draft.SentAtUtc);
        Rules.ValidateAndThrowDomain(message);
        return message;
    }
}
