using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using com.jobsite.chat.Shared.Contracts.Repositories;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Repositories;

public sealed class ChatMessageRepository(IDataContext<ChatDbContext> data) : IChatMessageRepository
{

    public ChatMessageRepository(ChatDbContext db) : this(new DataContext<ChatDbContext>(db)) { }

    public Task AddAsync(ChatMessage message, CancellationToken ct) =>
        data.Insert<ChatMessage, Guid>(message, ct);

    public async Task<IReadOnlyList<ChatMessage>> GetLatestAsync(Guid roomId, int count, CancellationToken ct)
    {
        if (count <= 0)
        {
            return [];
        }

        IQueryable<ChatMessage> newestFirstQuery =
            from message in data.GetEntities<ChatMessage>()
            where message.RoomId == roomId
            orderby message.SentAtUtc descending, message.Id descending
            select message;

        List<ChatMessage> newestFirst = await newestFirstQuery.Take(count).ToListAsync(ct);

        newestFirst.Reverse();
        return newestFirst;
    }
}
