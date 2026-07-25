using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Shared.Abstractions;

// port implemented by Repository (EF) layer.
public interface IChatRoomRepository
{
    // Guid.Empty => false (by contract).
    Task<bool> ExistsAsync(Guid roomId, CancellationToken ct);
    
    Task<IReadOnlyList<ChatRoom>> GetAllAsync(CancellationToken ct);
    
    // Persists immediately (no separate UoW).
    Task AddAsync(ChatRoom room, CancellationToken ct);
}
