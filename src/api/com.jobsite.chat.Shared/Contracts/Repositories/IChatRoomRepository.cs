using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Shared.Contracts.Repositories;

// port implemented by Repository (EF) layer.
public interface IChatRoomRepository
{
    // Guid.Empty => false (by contract).
    Task<bool> ExistsAsync(Guid roomId, CancellationToken ct);
    
    Task<IReadOnlyList<ChatRoom>> GetAllAsync(CancellationToken ct);

    // True if a room already exists with this name (trimmed, case-insensitive).
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct);

    // Persists immediately (no separate UoW).
    Task AddAsync(ChatRoom room, CancellationToken ct);
}
