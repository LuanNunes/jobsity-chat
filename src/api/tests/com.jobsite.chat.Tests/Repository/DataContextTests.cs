using System.Data.Common;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.ValueObjects;
using com.jobsite.chat.Repository;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using com.jobsite.chat.Shared.Contracts.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace com.jobsite.chat.Tests.Repository;

public sealed class DataContextTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    private static IDataContext<ChatDbContext> NewData(ChatDbContext context) =>
        new DataContext<ChatDbContext>(context);

    private static ChatMessage NewUserMessage(
        Guid roomId, string content, DateTimeOffset sentAt, string userId = "user-1", string displayName = "Ana")
    {
        FakeTimeProvider clock = new(sentAt);
        NewChatMessageDto draft = NewChatMessageDto.Create(roomId, MessageAuthor.User(userId, displayName), content, clock);
        return ChatMessage.Create(draft);
    }

    [Fact]
    public async Task GetEntities_SeededRooms_ReturnsAllAndComposes()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom first = ChatRoom.Create("General", BaseTime);
        ChatRoom second = ChatRoom.Create("Trading", BaseTime.AddMinutes(1));

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            writeContext.Rooms.AddRange(first, second);
            await writeContext.SaveChangesAsync();
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(readContext);

        int total = await data.GetEntities<ChatRoom>().CountAsync();
        int named = await data.GetEntities<ChatRoom>().Where(r => r.Name == "General").CountAsync();

        Assert.Equal(2, total);
        Assert.Equal(1, named);
    }

    [Fact]
    public async Task GetEntities_WithPredicate_FiltersAndNullPredicateReturnsAll()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom general = ChatRoom.Create("General", BaseTime);
        ChatRoom trading = ChatRoom.Create("Trading", BaseTime.AddMinutes(1));

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            writeContext.Rooms.AddRange(general, trading);
            await writeContext.SaveChangesAsync();
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(readContext);

        List<ChatRoom> filtered = await data.GetEntities<ChatRoom>(r => r.Name == "Trading").ToListAsync();
        List<ChatRoom> all = await data.GetEntities<ChatRoom>(predicate: null).ToListAsync();

        Assert.Single(filtered);
        Assert.Equal(trading.Id, filtered[0].Id);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetEntities_WithIncludeDelegate_ExecutesAndPopulatesAuthor()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage message = NewUserMessage(roomId, "hello", BaseTime, userId: "user-9", displayName: "Ines");

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            writeContext.Messages.Add(message);
            await writeContext.SaveChangesAsync();
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(readContext);

        ChatMessage viaDelegate = await data
            .GetEntities<ChatMessage>(m => m.RoomId == roomId, q => q.Include(m => m.Author))
            .SingleAsync();
        ChatMessage viaFluent = await data
            .GetEntities<ChatMessage>()
            .Include(m => m.Author)
            .SingleAsync(m => m.Id == message.Id);

        Assert.NotNull(viaDelegate.Author);
        Assert.Equal("Ines", viaDelegate.Author.DisplayName);
        Assert.NotNull(viaFluent.Author);
        Assert.Equal("Ines", viaFluent.Author.DisplayName);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsEntity()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("General", BaseTime);

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            writeContext.Rooms.Add(room);
            await writeContext.SaveChangesAsync();
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(readContext);

        ChatRoom? found = await data.GetById<ChatRoom, Guid>(room.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(room.Id, found.Id);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        using SqliteInMemoryFixture fixture = new();
        await using ChatDbContext context = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(context);

        ChatRoom? found = await data.GetById<ChatRoom, Guid>(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetById_GuidEmpty_ReturnsNull()
    {
        using SqliteInMemoryFixture fixture = new();
        await using ChatDbContext context = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(context);

        ChatRoom? found = await data.GetById<ChatRoom, Guid>(Guid.Empty, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetById_CalledTwice_ReturnsSameTrackedInstance()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("General", BaseTime);

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            writeContext.Rooms.Add(room);
            await writeContext.SaveChangesAsync();
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(readContext);

        ChatRoom? first = await data.GetById<ChatRoom, Guid>(room.Id, CancellationToken.None);
        ChatRoom? second = await data.GetById<ChatRoom, Guid>(room.Id, CancellationToken.None);

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task BulkInsert_ThreeRooms_AllVisibleFromFreshContext()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom one = ChatRoom.Create("One", BaseTime);
        ChatRoom two = ChatRoom.Create("Two", BaseTime.AddMinutes(1));
        ChatRoom three = ChatRoom.Create("Three", BaseTime.AddMinutes(2));

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);
            await data.BulkInsert<ChatRoom, Guid>([one, two, three], CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        int count = await readContext.Rooms.AsNoTracking().CountAsync();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task BulkInsert_EmptyCollection_PersistsNothing()
    {
        using SqliteInMemoryFixture fixture = new();

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);
            await data.BulkInsert<ChatRoom, Guid>([], CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        int count = await readContext.Rooms.AsNoTracking().CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task BulkInsert_ChatMessage_PersistsOwnedAuthorColumns()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage message = NewUserMessage(roomId, "owned graph", BaseTime, userId: "user-77", displayName: "Rui");

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);
            await data.BulkInsert<ChatMessage, Guid>([message], CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        ChatMessage persisted = await readContext.Messages.AsNoTracking().SingleAsync(m => m.Id == message.Id);

        Assert.Equal("user-77", persisted.Author.UserId);
        Assert.Equal("Rui", persisted.Author.DisplayName);
        Assert.False(persisted.Author.IsBot);
    }

    [Fact]
    public async Task Insert_SingleRoom_IsVisibleFromFreshContext()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("Solo", BaseTime);

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);
            await data.Insert<ChatRoom, Guid>(room, CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        ChatRoom persisted = await readContext.Rooms.AsNoTracking().SingleAsync(r => r.Id == room.Id);

        Assert.Equal("Solo", persisted.Name);
    }

    [Fact]
    public async Task Insert_ChatMessage_PersistsOwnedAuthorColumns()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage message = NewUserMessage(roomId, "single insert", BaseTime, userId: "user-9", displayName: "Ivo");

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);
            await data.Insert<ChatMessage, Guid>(message, CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        ChatMessage persisted = await readContext.Messages.AsNoTracking().SingleAsync(m => m.Id == message.Id);

        Assert.Equal("user-9", persisted.Author.UserId);
        Assert.Equal("Ivo", persisted.Author.DisplayName);
        Assert.False(persisted.Author.IsBot);
    }

    [Fact]
    public async Task BulkDelete_ByIds_DeletesTargetsImmediatelyLeavingOthers()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom one = ChatRoom.Create("One", BaseTime);
        ChatRoom two = ChatRoom.Create("Two", BaseTime.AddMinutes(1));
        ChatRoom three = ChatRoom.Create("Three", BaseTime.AddMinutes(2));

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.AddRange(one, two, three);
            await seedContext.SaveChangesAsync();
        }

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);

            await data.BulkDelete<ChatRoom, Guid>([one.Id, two.Id], CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        List<Guid> remaining =
            await (from room in readContext.Rooms.AsNoTracking()
                   select room.Id).ToListAsync();

        Assert.Single(remaining);
        Assert.Equal(three.Id, remaining[0]);
    }

    [Fact]
    public async Task BulkDelete_ByEmptyIds_DeletesNothing()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("Keep", BaseTime);

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.Add(room);
            await seedContext.SaveChangesAsync();
        }

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);
            await data.BulkDelete<ChatRoom, Guid>([], CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        int count = await readContext.Rooms.AsNoTracking().CountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task BulkDelete_ByUnknownIds_DeletesNothing()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("Keep", BaseTime);

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.Add(room);
            await seedContext.SaveChangesAsync();
        }

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);
            await data.BulkDelete<ChatRoom, Guid>([Guid.NewGuid(), Guid.NewGuid()], CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        int count = await readContext.Rooms.AsNoTracking().CountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task BulkDelete_ByPredicate_DeletesOnlyMatchingImmediately()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom keep = ChatRoom.Create("Keep", BaseTime);
        ChatRoom drop = ChatRoom.Create("Drop", BaseTime.AddMinutes(1));

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.AddRange(keep, drop);
            await seedContext.SaveChangesAsync();
        }

        await using (ChatDbContext writeContext = fixture.NewChatContext())
        {
            IDataContext<ChatDbContext> data = NewData(writeContext);
            await data.BulkDelete<ChatRoom, Guid>(r => r.Name == "Drop", CancellationToken.None);
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        List<ChatRoom> remaining = await readContext.Rooms.AsNoTracking().ToListAsync();

        Assert.Single(remaining);
        Assert.Equal(keep.Id, remaining[0].Id);
    }

    [Fact]
    public async Task BatchUpdate_SetProperty_UpdatesImmediatelyAndBypassesTracker()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom target = ChatRoom.Create("Original", BaseTime);
        ChatRoom other = ChatRoom.Create("Other", BaseTime.AddMinutes(1));

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.AddRange(target, other);
            await seedContext.SaveChangesAsync();
        }

        await using ChatDbContext writeContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(writeContext);
        ChatRoom tracked = await writeContext.Rooms.SingleAsync(r => r.Id == target.Id);

        await data.BatchUpdate<ChatRoom, Guid>(
            r => r.Id == target.Id,
            s => s.SetProperty(r => r.Name, "renamed"),
            CancellationToken.None);

        await using ChatDbContext readContext = fixture.NewChatContext();
        ChatRoom persistedTarget = await readContext.Rooms.AsNoTracking().SingleAsync(r => r.Id == target.Id);
        ChatRoom persistedOther = await readContext.Rooms.AsNoTracking().SingleAsync(r => r.Id == other.Id);

        Assert.Equal("renamed", persistedTarget.Name);
        Assert.Equal("Other", persistedOther.Name);
        Assert.Equal("Original", tracked.Name);
    }

    [Fact]
    public async Task SetEntityState_Added_MarksAddedThenPersistsOnSave()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("Staged", BaseTime);

        await using ChatDbContext writeContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(writeContext);

        await data.SetEntityState<ChatRoom, Guid>(room, EntityState.Added);
        Assert.Equal(EntityState.Added, writeContext.Entry(room).State);

        await data.SaveChangesAsync(CancellationToken.None);

        await using ChatDbContext readContext = fixture.NewChatContext();
        ChatRoom? persisted = await readContext.Rooms.AsNoTracking().SingleOrDefaultAsync(r => r.Id == room.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task SetUpdateEntityState_AttachedRoom_MarksModifiedAndSaveCompletes()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("Modifiable", BaseTime);

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.Add(room);
            await seedContext.SaveChangesAsync();
        }

        await using ChatDbContext writeContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(writeContext);
        writeContext.Rooms.Attach(room);

        await data.SetUpdateEntityState<ChatRoom, Guid>(room);
        Assert.Equal(EntityState.Modified, writeContext.Entry(room).State);

        int affected = await data.SaveChangesAsync(CancellationToken.None);
        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task RemoveEntity_ExistingId_DeletesOnlyAfterSaveChanges()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("Doomed", BaseTime);

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.Add(room);
            await seedContext.SaveChangesAsync();
        }

        await using ChatDbContext writeContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(writeContext);

        await data.RemoveEntity<ChatRoom, Guid>(room.Id, CancellationToken.None);

        await using (ChatDbContext beforeSaveContext = fixture.NewChatContext())
        {
            bool visibleBeforeSave = await beforeSaveContext.Rooms.AsNoTracking().AnyAsync(r => r.Id == room.Id);
            Assert.True(visibleBeforeSave);
        }

        await data.SaveChangesAsync(CancellationToken.None);

        await using ChatDbContext afterSaveContext = fixture.NewChatContext();
        bool visibleAfterSave = await afterSaveContext.Rooms.AsNoTracking().AnyAsync(r => r.Id == room.Id);
        Assert.False(visibleAfterSave);
    }

    [Fact]
    public async Task RemoveEntity_UnknownId_IsNoOpAndSavesZero()
    {
        using SqliteInMemoryFixture fixture = new();

        await using ChatDbContext writeContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(writeContext);

        await data.RemoveEntity<ChatRoom, Guid>(Guid.NewGuid(), CancellationToken.None);
        int affected = await data.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task FromSql_ParameterizedQuery_ReturnsMatch()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("Analytics", BaseTime);

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.Add(room);
            await seedContext.SaveChangesAsync();
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(readContext);

        ChatRoom match = await data
            .FromSql<ChatRoom>("SELECT * FROM Rooms WHERE Name = {0}", "Analytics")
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(room.Id, match.Id);
    }

    [Fact]
    public async Task FromSql_ValueWithSingleQuote_IsParameterizedSafely()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("O'Brien", BaseTime);

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.Add(room);
            await seedContext.SaveChangesAsync();
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(readContext);

        List<ChatRoom> matches = await data
            .FromSql<ChatRoom>("SELECT * FROM Rooms WHERE Name = {0}", "O'Brien")
            .AsNoTracking()
            .ToListAsync();

        Assert.Single(matches);
        Assert.Equal(room.Id, matches[0].Id);
    }

    [Fact]
    public void GetConnectionString_ChatContext_ReturnsInMemorySqliteString()
    {
        using SqliteInMemoryFixture fixture = new();
        using ChatDbContext context = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(context);

        string connectionString = data.GetConnectionString();

        Assert.Contains(":memory:", connectionString);
    }

    [Fact]
    public async Task CreateCommand_CountRooms_ReturnsSeededCount()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom one = ChatRoom.Create("One", BaseTime);
        ChatRoom two = ChatRoom.Create("Two", BaseTime.AddMinutes(1));

        await using (ChatDbContext seedContext = fixture.NewChatContext())
        {
            seedContext.Rooms.AddRange(one, two);
            await seedContext.SaveChangesAsync();
        }

        await using ChatDbContext context = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(context);

        using DbCommand command = data.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Rooms";
        long count = Convert.ToInt64(await command.ExecuteScalarAsync());

        Assert.Equal(2L, count);
    }

    [Fact]
    public async Task SaveChanges_StagedInsert_PersistsAndReturnsCount()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("SyncSaved", BaseTime);

        await using ChatDbContext writeContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(writeContext);
        writeContext.Rooms.Add(room);

        int affected = data.SaveChanges();

        Assert.Equal(1, affected);

        await using ChatDbContext readContext = fixture.NewChatContext();
        bool persisted = await readContext.Rooms.AsNoTracking().AnyAsync(r => r.Id == room.Id);
        Assert.True(persisted);
    }

    [Fact]
    public async Task SaveChangesAsync_StagedInsert_PersistsAndReturnsCount()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("AsyncSaved", BaseTime);

        await using ChatDbContext writeContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(writeContext);
        writeContext.Rooms.Add(room);

        int affected = await data.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task SaveChangesAsync_AlreadyCanceledToken_ThrowsOperationCanceled()
    {
        using SqliteInMemoryFixture fixture = new();
        ChatRoom room = ChatRoom.Create("NeverSaved", BaseTime);

        await using ChatDbContext writeContext = fixture.NewChatContext();
        IDataContext<ChatDbContext> data = NewData(writeContext);
        writeContext.Rooms.Add(room);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => data.SaveChangesAsync(cts.Token));
    }

    [Fact]
    public void AddRepositoryLayer_Registration_ResolvesDataContextsAndRepositories()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ChatDatabase"] = "Data Source=:memory:",
            })
            .Build();

        ServiceCollection services = new();
        services.AddRepositoryLayer(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IDataContext<ChatDbContext> chatData = scope.ServiceProvider.GetRequiredService<IDataContext<ChatDbContext>>();
        IDataContext<AppIdentityDbContext> identityData =
            scope.ServiceProvider.GetRequiredService<IDataContext<AppIdentityDbContext>>();
        IChatRoomRepository roomRepository = scope.ServiceProvider.GetRequiredService<IChatRoomRepository>();
        IChatMessageRepository messageRepository = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();

        Assert.NotNull(chatData);
        Assert.NotNull(identityData);
        Assert.NotNull(roomRepository);
        Assert.NotNull(messageRepository);
    }
}
