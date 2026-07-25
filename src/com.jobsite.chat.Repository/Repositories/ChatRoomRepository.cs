using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Repositories;

public sealed class ChatRoomRepository(ChatDbContext db) : IChatRoomRepository
{
    public Task<bool> ExistsAsync(Guid roomId, CancellationToken ct)
        => roomId == Guid.Empty
            ? Task.FromResult(false)
            : db.Rooms.AnyAsync(r => r.Id == roomId, ct);

    public async Task<IReadOnlyList<ChatRoom>> GetAllAsync(CancellationToken ct)
        => await db.Rooms.AsNoTracking()
            .OrderBy(r => r.CreatedAtUtc).ThenBy(r => r.Id)
            .ToListAsync(ct);

    public async Task AddAsync(ChatRoom room, CancellationToken ct)
    {
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
    }
}
