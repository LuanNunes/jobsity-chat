using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using com.jobsite.chat.Shared.Contracts.Repositories;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Repositories;

public sealed class ChatRoomRepository(IDataContext<ChatDbContext> data) : IChatRoomRepository
{

    public ChatRoomRepository(ChatDbContext db) : this(new DataContext<ChatDbContext>(db)) { }

    public Task<bool> ExistsAsync(Guid roomId, CancellationToken ct) =>
        roomId == Guid.Empty
            ? Task.FromResult(false)
            : data.GetEntities<ChatRoom>().AnyAsync(r => r.Id == roomId, ct);

    public async Task<IReadOnlyList<ChatRoom>> GetAllAsync(CancellationToken ct)
    {
        IQueryable<ChatRoom> roomsByCreation =
            from room in data.GetEntities<ChatRoom>()
            orderby room.CreatedAtUtc, room.Id
            select room;

        return await roomsByCreation.ToListAsync(ct);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
    {
        string trimmed = name.Trim();

        IQueryable<ChatRoom> matching =
            from room in data.GetEntities<ChatRoom>()
            where EF.Functions.Collate(room.Name, "NOCASE") == trimmed
            select room;

        return matching.AnyAsync(ct);
    }

    public Task AddAsync(ChatRoom room, CancellationToken ct) =>
        data.Insert<ChatRoom, Guid>(room, ct);
}
