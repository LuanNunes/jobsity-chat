using FluentValidation;
using com.jobsite.chat.Domain.Abstractions;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Rules;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Domain.Entities;

public sealed class ChatMessage : IEntityValidator<Guid>
{
    private const int MaxContentLength = 1000;

    private ChatMessage(Guid id, Guid roomId, MessageAuthor author, string content, DateTimeOffset sentAtUtc)
    {
        Id = id;
        RoomId = roomId;
        Author = author;
        Content = content;
        SentAtUtc = sentAtUtc;
    }

    private ChatMessage() { Author = null!; Content = null!; }

    public Guid Id { get; }
    public Guid RoomId { get; }
    public MessageAuthor Author { get; }
    public string Content { get; }
    public DateTimeOffset SentAtUtc { get; }

    private static readonly InlineValidator<ChatMessage> Rules = new()
    {
        v => v.RuleFor(m => m.RoomId).NotEmpty(),
        v => v.RuleFor(m => m.Author).NotNull(),
        v => v.RuleFor(m => m.Content).NotEmpty().MaximumLength(MaxContentLength),
    };

    public static ChatMessage Create(NewChatMessageDto draft)
    {
        ChatMessage message = new(
            Guid.CreateVersion7(), draft.RoomId, draft.Author!, draft.Content?.Trim() ?? string.Empty, draft.SentAtUtc);
        Rules.ValidateAndThrowDomain(message);
        return message;
    }
}
