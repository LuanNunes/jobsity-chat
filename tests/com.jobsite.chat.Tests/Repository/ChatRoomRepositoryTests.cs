using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Repositories;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Tests.Repository;

// Spec §4 behaviors 1-8: ChatRoomRepository against SQLite in-memory.
public sealed class ChatRoomRepositoryTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    // Behavior 1: AddAsync persists immediately; a second context on the same connection sees the room.
    [Fact]
    public async Task AddAsync_PersistedRoom_IsVisibleFromSecondContext()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("General", BaseTime);

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IChatRoomRepository repository = new ChatRoomRepository(writeContext);
            await repository.AddAsync(room, CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        ChatRoom? persisted = await readContext.Rooms.AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == room.Id);
        Assert.NotNull(persisted);
    }

    // Behavior 2: Room round-trip preserves Id, Name, CreatedAtUtc (offset zero — validates UtcTicks converter).
    [Fact]
    public async Task AddAsync_RoundTrip_PreservesIdNameAndCreatedAtUtc()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("Trading Floor", BaseTime);

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IChatRoomRepository repository = new ChatRoomRepository(writeContext);
            await repository.AddAsync(room, CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        ChatRoom persisted = await readContext.Rooms.AsNoTracking().SingleAsync(r => r.Id == room.Id);

        Assert.Equal(room.Id, persisted.Id);
        Assert.Equal("Trading Floor", persisted.Name);
        Assert.Equal(BaseTime, persisted.CreatedAtUtc);
        Assert.Equal(TimeSpan.Zero, persisted.CreatedAtUtc.Offset);
    }

    // Behavior 3: ExistsAsync -> true for a persisted id.
    [Fact]
    public async Task ExistsAsync_PersistedRoom_ReturnsTrue()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("General", BaseTime);

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IChatRoomRepository writeRepository = new ChatRoomRepository(writeContext);
            await writeRepository.AddAsync(room, CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatRoomRepository repository = new ChatRoomRepository(readContext);
        bool exists = await repository.ExistsAsync(room.Id, CancellationToken.None);

        Assert.True(exists);
    }

    // Behavior 4: ExistsAsync -> false for an unknown id.
    [Fact]
    public async Task ExistsAsync_UnknownRoom_ReturnsFalse()
    {
        using SqliteInMemoryFixture fixture = new();
        await using ChatDbContext context = fixture.NewChatContext();
        IChatRoomRepository repository = new ChatRoomRepository(context);

        bool exists = await repository.ExistsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(exists);
    }

    // Behavior 5: ExistsAsync(Guid.Empty) -> false (by contract).
    [Fact]
    public async Task ExistsAsync_GuidEmpty_ReturnsFalse()
    {
        using SqliteInMemoryFixture fixture = new();
        await using ChatDbContext context = fixture.NewChatContext();
        IChatRoomRepository repository = new ChatRoomRepository(context);

        bool exists = await repository.ExistsAsync(Guid.Empty, CancellationToken.None);

        Assert.False(exists);
    }

    // Behavior 6: GetAllAsync on empty DB -> empty list (not null).
    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyListNotNull()
    {
        using SqliteInMemoryFixture fixture = new();
        await using ChatDbContext context = fixture.NewChatContext();
        IChatRoomRepository repository = new ChatRoomRepository(context);

        IReadOnlyList<ChatRoom> rooms = await repository.GetAllAsync(CancellationToken.None);

        Assert.NotNull(rooms);
        Assert.Empty(rooms);
    }

    // Behavior 7: GetAllAsync returns all rooms ordered by CreatedAtUtc ascending (inserted out of order).
    [Fact]
    public async Task GetAllAsync_MultipleRooms_ReturnsAllOrderedByCreatedAtUtcAscending()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom middle = ChatRoom.Create("Middle", BaseTime.AddMinutes(10));
        ChatRoom oldest = ChatRoom.Create("Oldest", BaseTime);
        ChatRoom newest = ChatRoom.Create("Newest", BaseTime.AddMinutes(20));

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IChatRoomRepository writeRepository = new ChatRoomRepository(writeContext);
            await writeRepository.AddAsync(middle, CancellationToken.None);
            await writeRepository.AddAsync(newest, CancellationToken.None);
            await writeRepository.AddAsync(oldest, CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatRoomRepository repository = new ChatRoomRepository(readContext);
        IReadOnlyList<ChatRoom> rooms = await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(3, rooms.Count);
        Assert.Equal(oldest.Id, rooms[0].Id);
        Assert.Equal(middle.Id, rooms[1].Id);
        Assert.Equal(newest.Id, rooms[2].Id);
    }

    // Behavior 8: GetAllAsync leaves the change tracker empty (no-tracking reads).
    [Fact]
    public async Task GetAllAsync_AfterRead_LeavesChangeTrackerEmpty()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("General", BaseTime);

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IChatRoomRepository writeRepository = new ChatRoomRepository(writeContext);
            await writeRepository.AddAsync(room, CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatRoomRepository repository = new ChatRoomRepository(readContext);
        await repository.GetAllAsync(CancellationToken.None);

        Assert.Empty(readContext.ChangeTracker.Entries());
    }
}
