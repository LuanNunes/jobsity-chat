using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Shared.Contracts.Repositories;

public interface IChatMessageRepository
{

    Task AddAsync(ChatMessage message, CancellationToken ct);

    Task<IReadOnlyList<ChatMessage>> GetLatestAsync(Guid roomId, int count, CancellationToken ct);
}
