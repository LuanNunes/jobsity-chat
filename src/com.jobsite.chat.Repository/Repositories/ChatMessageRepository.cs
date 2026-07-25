using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Repositories;

public sealed class ChatMessageRepository(ChatDbContext db) : IChatMessageRepository
{
    public async Task AddAsync(ChatMessage message, CancellationToken ct)
    {
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetLatestAsync(Guid roomId, int count, CancellationToken ct)
    {
        if (count <= 0)          // LOAD-BEARING: SQLite treats LIMIT -1 as "no limit"
        {
            return [];
        }

        IQueryable<ChatMessage> newestFirstQuery =
            from message in db.Messages.AsNoTracking()
            where message.RoomId == roomId
            orderby message.SentAtUtc descending, message.Id descending   // => ascending after reverse (ties by Id asc)
            select message;

        List<ChatMessage> newestFirst = await newestFirstQuery.Take(count).ToListAsync(ct);

        newestFirst.Reverse();                               // oldest->newest of the window
        return newestFirst;
    }
}
