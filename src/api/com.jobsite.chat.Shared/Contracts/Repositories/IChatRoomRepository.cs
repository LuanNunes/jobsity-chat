using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Shared.Contracts.Repositories;

public interface IChatRoomRepository
{

    Task<bool> ExistsAsync(Guid roomId, CancellationToken ct);

    Task<IReadOnlyList<ChatRoom>> GetAllAsync(CancellationToken ct);

    Task<bool> ExistsByNameAsync(string name, CancellationToken ct);

    Task AddAsync(ChatRoom room, CancellationToken ct);
}
