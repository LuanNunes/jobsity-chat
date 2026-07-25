using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.ValueObjects;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Repositories;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace com.jobsite.chat.Tests.Repository;

// Spec §4 behaviors 9-20: ChatMessageRepository against SQLite in-memory.
public sealed class ChatMessageRepositoryTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    // Builds a ChatMessage with a controlled SentAtUtc via FakeTimeProvider + the domain factory.
    private static ChatMessage NewUserMessage(Guid roomId, string content, DateTimeOffset sentAt, string userId = "user-1", string displayName = "Ana")
    {
        FakeTimeProvider clock = new(sentAt);
        NewChatMessageDto draft = NewChatMessageDto.Create(roomId, MessageAuthor.User(userId, displayName), content, clock);
        return ChatMessage.Create(draft);
    }

    private static ChatMessage NewBotMessage(Guid roomId, string content, DateTimeOffset sentAt)
    {
        FakeTimeProvider clock = new(sentAt);
        NewChatMessageDto draft = NewChatMessageDto.Create(roomId, MessageAuthor.Bot(), content, clock);
        return ChatMessage.Create(draft);
    }

    private static async Task AddMessagesAsync(SqliteInMemoryFixture fixture, params ChatMessage[] messages)
    {
        await using ChatDbContext writeContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(writeContext);
        foreach (ChatMessage message in messages)
        {
            await repository.AddAsync(message, CancellationToken.None);
        }
    }

    // Behavior 9: AddAsync persists immediately; a second context on the same connection sees the message.
    [Fact]
    public async Task AddAsync_PersistedMessage_IsVisibleFromSecondContext()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage message = NewUserMessage(roomId, "hello", BaseTime);

        await AddMessagesAsync(fixture, message);

        await using ChatDbContext readContext = fixture.NewChatContext();
        ChatMessage? persisted = await readContext.Messages.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == message.Id);
        Assert.NotNull(persisted);
    }

    // Behavior 10: User-message round-trip preserves all fields; IsBot false — proves owned-type mapping + ctor rebinding.
    [Fact]
    public async Task GetLatestAsync_UserMessageRoundTrip_PreservesAllFields()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage message = NewUserMessage(roomId, "hello world", BaseTime, userId: "user-42", displayName: "Beatriz");

        await AddMessagesAsync(fixture, message);

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        ChatMessage persisted = Assert.Single(result);
        Assert.Equal(message.Id, persisted.Id);
        Assert.Equal(roomId, persisted.RoomId);
        Assert.Equal("hello world", persisted.Content);
        Assert.Equal(BaseTime, persisted.SentAtUtc);
        Assert.Equal("user-42", persisted.Author.UserId);
        Assert.Equal("Beatriz", persisted.Author.DisplayName);
        Assert.False(persisted.Author.IsBot);
    }

    // Behavior 11: Bot-message round-trip — Author.UserId null, IsBot true, DisplayName "StockBot" (nullable owned column).
    [Fact]
    public async Task GetLatestAsync_BotMessageRoundTrip_PreservesNullUserIdAndIsBot()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage message = NewBotMessage(roomId, "AAPL.US quote is 100.00", BaseTime);

        await AddMessagesAsync(fixture, message);

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        ChatMessage persisted = Assert.Single(result);
        Assert.Null(persisted.Author.UserId);
        Assert.True(persisted.Author.IsBot);
        Assert.Equal("StockBot", persisted.Author.DisplayName);
    }

    // Behavior 12: GetLatestAsync for an unknown room and for Guid.Empty -> empty list.
    [Fact]
    public async Task GetLatestAsync_UnknownRoom_ReturnsEmptyList()
    {
        using SqliteInMemoryFixture fixture = new();
        await using ChatDbContext context = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(context);

        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(Guid.CreateVersion7(), 50, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // Behavior 12 (companion): Guid.Empty room -> empty list.
    [Fact]
    public async Task GetLatestAsync_GuidEmptyRoom_ReturnsEmptyList()
    {
        using SqliteInMemoryFixture fixture = new();
        await using ChatDbContext context = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(context);

        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(Guid.Empty, 50, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // Behavior 13: More than `count` messages -> exactly `count`, the NEWEST (oldest excluded).
    [Fact]
    public async Task GetLatestAsync_MoreThanCount_ReturnsNewestCountOnly()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage oldest = NewUserMessage(roomId, "m0", BaseTime);
        ChatMessage middle = NewUserMessage(roomId, "m1", BaseTime.AddMinutes(1));
        ChatMessage newest = NewUserMessage(roomId, "m2", BaseTime.AddMinutes(2));

        await AddMessagesAsync(fixture, oldest, middle, newest);

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 2, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, m => m.Id == oldest.Id);
        Assert.Contains(result, m => m.Id == middle.Id);
        Assert.Contains(result, m => m.Id == newest.Id);
    }

    // Behavior 14: Returned window is ASCENDING by SentAtUtc (inserted shuffled).
    [Fact]
    public async Task GetLatestAsync_ShuffledInserts_ReturnsAscendingBySentAtUtc()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage second = NewUserMessage(roomId, "m1", BaseTime.AddMinutes(1));
        ChatMessage fourth = NewUserMessage(roomId, "m3", BaseTime.AddMinutes(3));
        ChatMessage first = NewUserMessage(roomId, "m0", BaseTime);
        ChatMessage third = NewUserMessage(roomId, "m2", BaseTime.AddMinutes(2));

        await AddMessagesAsync(fixture, second, fourth, first, third);

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        Assert.Equal(4, result.Count);
        Assert.Equal(first.Id, result[0].Id);
        Assert.Equal(second.Id, result[1].Id);
        Assert.Equal(third.Id, result[2].Id);
        Assert.Equal(fourth.Id, result[3].Id);
    }

    // Behavior 15: Fewer than `count` messages -> all of them, ascending.
    [Fact]
    public async Task GetLatestAsync_FewerThanCount_ReturnsAllAscending()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        ChatMessage first = NewUserMessage(roomId, "m0", BaseTime);
        ChatMessage second = NewUserMessage(roomId, "m1", BaseTime.AddMinutes(1));

        await AddMessagesAsync(fixture, second, first);

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(first.Id, result[0].Id);
        Assert.Equal(second.Id, result[1].Id);
    }

    // Behavior 16: Identical SentAtUtc -> ordered by Id ascending. The test can't control the
    // Id (ChatMessage.Create generates it), and two back-to-back Guid.CreateVersion7() calls are
    // NOT guaranteed monotonic within the same millisecond, so we DERIVE the expected order from
    // the actual ids rather than asserting a creation-order precondition.
    [Fact]
    public async Task GetLatestAsync_IdenticalSentAtUtc_OrdersByIdAscending()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        // Same timestamp; whichever id is smaller must come first regardless of creation order.
        ChatMessage messageA = NewUserMessage(roomId, "a", BaseTime);
        ChatMessage messageB = NewUserMessage(roomId, "b", BaseTime);

        ChatMessage smallerId = messageA.Id.CompareTo(messageB.Id) < 0 ? messageA : messageB;
        ChatMessage largerId = ReferenceEquals(smallerId, messageA) ? messageB : messageA;

        // Insert larger-id first to prove ordering is by Id, not insert order.
        await AddMessagesAsync(fixture, largerId, smallerId);

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(smallerId.Id, result[0].Id);
        Assert.Equal(largerId.Id, result[1].Id);
    }

    // Behavior 17: Messages of other rooms are excluded.
    [Fact]
    public async Task GetLatestAsync_OtherRoomsPresent_ExcludesThem()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        Guid otherRoomId = Guid.CreateVersion7();
        ChatMessage mine = NewUserMessage(roomId, "mine", BaseTime);
        ChatMessage theirs = NewUserMessage(otherRoomId, "theirs", BaseTime.AddMinutes(1));

        await AddMessagesAsync(fixture, mine, theirs);

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        ChatMessage only = Assert.Single(result);
        Assert.Equal(mine.Id, only.Id);
    }

    // Behavior 18: count == 0 -> empty list (SQLite LIMIT -1 regression guard).
    [Fact]
    public async Task GetLatestAsync_CountZero_ReturnsEmptyList()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        await AddMessagesAsync(fixture, NewUserMessage(roomId, "m0", BaseTime));

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 0, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // Behavior 18 (companion): count < 0 -> empty list.
    [Fact]
    public async Task GetLatestAsync_CountNegative_ReturnsEmptyList()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        await AddMessagesAsync(fixture, NewUserMessage(roomId, "m0", BaseTime));

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, -5, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // Behavior 19: GetLatestAsync leaves the change tracker empty (no-tracking reads).
    [Fact]
    public async Task GetLatestAsync_AfterRead_LeavesChangeTrackerEmpty()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        await AddMessagesAsync(fixture, NewUserMessage(roomId, "m0", BaseTime));

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    // Behavior 20: Rehydration bypasses validation — a raw 1001-char row round-trips WITHOUT throwing DomainException.
    [Fact]
    public async Task GetLatestAsync_RowExceedingDomainMaxLength_RoundTripsWithoutDomainException()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();
        Guid messageId = Guid.CreateVersion7();
        string oversizedContent = new string('x', 1001);

        await using (SqliteCommand insert = fixture.ChatConnection.CreateCommand())
        {
            insert.CommandText =
                "INSERT INTO Messages (Id, RoomId, Content, SentAtUtc, AuthorUserId, AuthorDisplayName) " +
                "VALUES ($id, $roomId, $content, $sentAt, $userId, $displayName)";
            // EF Core's SQLite provider stores Guids as UPPERCASE TEXT under case-sensitive BINARY
            // collation; match that format so the repository's parameterized WHERE finds this row.
            insert.Parameters.AddWithValue("$id", messageId.ToString().ToUpperInvariant());
            insert.Parameters.AddWithValue("$roomId", roomId.ToString().ToUpperInvariant());
            insert.Parameters.AddWithValue("$content", oversizedContent);
            insert.Parameters.AddWithValue("$sentAt", BaseTime.UtcTicks);
            insert.Parameters.AddWithValue("$userId", "user-1");
            insert.Parameters.AddWithValue("$displayName", "Ana");
            await insert.ExecuteNonQueryAsync();
        }

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        ChatMessage persisted = Assert.Single(result);
        Assert.Equal(1001, persisted.Content.Length);
    }
}
