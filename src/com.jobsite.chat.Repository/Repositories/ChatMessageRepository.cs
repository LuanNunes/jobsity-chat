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

        List<ChatMessage> newestFirst = await db.Messages.AsNoTracking()
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.SentAtUtc)
            .ThenByDescending(m => m.Id)                     // => ascending after reverse (ties by Id asc)
            .Take(count)
            .ToListAsync(ct);

        newestFirst.Reverse();                               // oldest->newest of the window
        return newestFirst;
    }
}
