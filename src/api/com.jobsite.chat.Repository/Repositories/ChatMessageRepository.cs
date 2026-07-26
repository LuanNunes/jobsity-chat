using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using com.jobsite.chat.Shared.Contracts.Repositories;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Repositories;

public sealed class ChatMessageRepository(IDataContext<ChatDbContext> data) : IChatMessageRepository
{
    // Compat ctor: keeps existing `new ChatMessageRepository(context)` call sites compiling.
    // Delegates to the primary ctor (one code path).
    public ChatMessageRepository(ChatDbContext db) : this(new DataContext<ChatDbContext>(db)) { }

    public Task AddAsync(ChatMessage message, CancellationToken ct) =>
        data.Insert<ChatMessage, Guid>(message, ct);

    public async Task<IReadOnlyList<ChatMessage>> GetLatestAsync(Guid roomId, int count, CancellationToken ct)
    {
        if (count <= 0)          // LOAD-BEARING: SQLite treats LIMIT -1 as "no limit"
        {
            return [];
        }

        IQueryable<ChatMessage> newestFirstQuery =
            from message in data.GetEntities<ChatMessage>()
            where message.RoomId == roomId
            orderby message.SentAtUtc descending, message.Id descending   // => ascending after reverse (ties by Id asc)
            select message;

        List<ChatMessage> newestFirst = await newestFirstQuery.Take(count).ToListAsync(ct);

        newestFirst.Reverse();                               // oldest->newest of the window
        return newestFirst;
    }
}
