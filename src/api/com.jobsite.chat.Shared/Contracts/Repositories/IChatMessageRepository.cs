using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Shared.Contracts.Repositories;

// port implemented by Repository (EF) layer.
public interface IChatMessageRepository
{
    // Persists immediately.
    Task AddAsync(ChatMessage message, CancellationToken ct);

    // Latest `count` messages of the room in ASCENDING chronological order
    // (oldest→newest of that window), ties broken by Id ascending.
    // Unknown roomId (including Guid.Empty) => empty list.
    Task<IReadOnlyList<ChatMessage>> GetLatestAsync(Guid roomId, int count, CancellationToken ct);
}
