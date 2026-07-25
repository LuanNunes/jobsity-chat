using FluentValidation;
using com.jobsite.chat.Domain.Validation;

namespace com.jobsite.chat.Domain.Entities;

// Core chat room entity; private ctor + static factory. EF Core materializes via
// the private ctor, bypassing the factory guards.
public sealed class ChatRoom
{
    public const int MaxNameLength = 100;

    private ChatRoom(Guid id, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public string Name { get; }           // trimmed
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
