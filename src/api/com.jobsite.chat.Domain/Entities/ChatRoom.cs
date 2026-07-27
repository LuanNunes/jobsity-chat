using FluentValidation;
using com.jobsite.chat.Domain.Abstractions;
using com.jobsite.chat.Domain.Rules;

namespace com.jobsite.chat.Domain.Entities;

public sealed class ChatRoom : IEntityValidator<Guid>
{
    private const int MaxNameLength = 100;

    private ChatRoom(Guid id, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public string Name { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private static readonly InlineValidator<ChatRoom> Rules = new()
    {
        v => v.RuleFor(r => r.Name).NotEmpty().MaximumLength(MaxNameLength),
    };

    public static ChatRoom Create(string name, DateTimeOffset createdAtUtc)
    {
        ChatRoom room = new(Guid.CreateVersion7(), name?.Trim() ?? string.Empty, createdAtUtc);
        Rules.ValidateAndThrowDomain(room);
        return room;
    }
}
