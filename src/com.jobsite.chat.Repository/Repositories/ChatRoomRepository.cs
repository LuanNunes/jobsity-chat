using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Repositories;

public sealed class ChatRoomRepository(IDataContext<ChatDbContext> data) : IChatRoomRepository
{
    // Compat ctor: keeps existing `new ChatRoomRepository(context)` call sites compiling.
    // Delegates to the primary ctor (one code path).
    public ChatRoomRepository(ChatDbContext db) : this(new DataContext<ChatDbContext>(db)) { }

    public Task<bool> ExistsAsync(Guid roomId, CancellationToken ct) =>
        roomId == Guid.Empty
            ? Task.FromResult(false)
            : data.GetEntities<ChatRoom>().AnyAsync(r => r.Id == roomId, ct);

    public async Task<IReadOnlyList<ChatRoom>> GetAllAsync(CancellationToken ct)
    {
        IQueryable<ChatRoom> roomsByCreation =
            from room in data.GetEntities<ChatRoom>().AsNoTracking()
            orderby room.CreatedAtUtc, room.Id
            select room;

        return await roomsByCreation.ToListAsync(ct);
    }

    public Task AddAsync(ChatRoom room, CancellationToken ct) =>
        data.BulkInsert<ChatRoom, Guid>([room], ct);
}
