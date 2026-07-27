using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.ValueObjects;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using com.jobsite.chat.Repository.Repositories;
using com.jobsite.chat.Shared.Contracts.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace com.jobsite.chat.Tests.Repository;

public sealed class ChatMessageRepositoryTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

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

    [Fact]
    public async Task GetLatestAsync_IdenticalSentAtUtc_OrdersByIdAscending()
    {
        using SqliteInMemoryFixture fixture = new();
        Guid roomId = Guid.CreateVersion7();

        ChatMessage messageA = NewUserMessage(roomId, "a", BaseTime);
        ChatMessage messageB = NewUserMessage(roomId, "b", BaseTime);

        ChatMessage smallerId = messageA.Id.CompareTo(messageB.Id) < 0 ? messageA : messageB;
        ChatMessage largerId = ReferenceEquals(smallerId, messageA) ? messageB : messageA;

        await AddMessagesAsync(fixture, largerId, smallerId);

        await using ChatDbContext readContext = fixture.NewChatContext();
        IChatMessageRepository repository = new ChatMessageRepository(readContext);
        IReadOnlyList<ChatMessage> result = await repository.GetLatestAsync(roomId, 50, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(smallerId.Id, result[0].Id);
        Assert.Equal(largerId.Id, result[1].Id);
    }

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
