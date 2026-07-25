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

    public static ChatRoom Create(string name, DateTimeOffset createdAtUtc)
    {
        string validName = Ensure.MaxLength(
            Ensure.NotNullOrWhiteSpace(name, nameof(name)),
            MaxNameLength,
            nameof(name));
        return new ChatRoom(Guid.CreateVersion7(), validName, createdAtUtc);
    }
}
