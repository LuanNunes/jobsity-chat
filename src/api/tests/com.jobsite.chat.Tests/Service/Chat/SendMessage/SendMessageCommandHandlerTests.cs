using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Service.Chat.Commands;
using com.jobsite.chat.Tests.Service.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace com.jobsite.chat.Tests.Service.Chat.SendMessage;

// Behaviors 19–24: SendMessageCommandHandler.
public class SendMessageCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 25, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeChatRoomRepository _rooms = new();
    private readonly FakeChatMessageRepository _messages = new();
    private readonly FakeStockQuoteRequestPublisher _publisher = new();
    private readonly FakeTimeProvider _clock = new(FixedNow);

    private SendMessageCommandHandler CreateHandler()
        => new(_rooms, _messages, _publisher, _clock);

    // Behavior 19
    [Fact]
    public async Task Handle_PlainContent_PersistsMessageAndReturnsPopulatedDto()
    {
        Guid roomId = Guid.NewGuid();
        _rooms.ExistsResult = true;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(roomId, "user-1", "Ana", "hello world");

        SendMessageResult result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(SendMessageOutcomeEnum.MessagePersisted, result.OutcomeEnum);
        Assert.Equal(1, _messages.AddAsyncCallCount);

        ChatMessage persisted = Assert.Single(_messages.Added);
        Assert.Equal(roomId, persisted.RoomId);
        Assert.Equal("Ana", persisted.Author.DisplayName);
        Assert.Equal("user-1", persisted.Author.UserId);
        Assert.Equal("hello world", persisted.Content);

        Assert.NotNull(result.Message);
        Assert.Equal(roomId, result.Message!.RoomId);
        Assert.Equal("Ana", result.Message.AuthorDisplayName);
        Assert.False(result.Message.IsFromBot);
        Assert.Equal("hello world", result.Message.Content);
    }

    // Behavior 19
    [Fact]
    public async Task Handle_PlainContent_NeverCallsPublisher()
    {
        _rooms.ExistsResult = true;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(Guid.NewGuid(), "user-1", "Ana", "hello world");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, _publisher.PublishCallCount);
    }

    // Behavior 20
    [Fact]
    public async Task Handle_PlainContent_PersistedSentAtUtcEqualsClock()
    {
        _rooms.ExistsResult = true;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(Guid.NewGuid(), "user-1", "Ana", "hello");

        SendMessageResult result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(FixedNow, _messages.Added.Single().SentAtUtc);
        Assert.Equal(FixedNow, result.Message!.SentAtUtc);
    }

    // Behavior 21
    [Fact]
    public async Task Handle_StockCommand_PublishesRequestAndDoesNotPersist()
    {
        Guid roomId = Guid.NewGuid();
        _rooms.ExistsResult = true;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(roomId, "user-1", "Ana", "/stock=aapl.us");

        SendMessageResult result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(SendMessageOutcomeEnum.StockCommandQueued, result.OutcomeEnum);
        Assert.Null(result.Message);
        Assert.Equal("aapl.us", result.StockCode);

        Assert.Equal(1, _publisher.PublishCallCount);
        StockQuoteRequestDto published = Assert.Single(_publisher.Published);
        Assert.Equal(roomId, published.RoomId);
        Assert.Equal("aapl.us", published.StockCode);

        Assert.Equal(0, _messages.AddAsyncCallCount);
    }

    // Behavior 22
    [Fact]
    public async Task Handle_UnknownCommand_PersistsNothingPublishesNothing()
    {
        _rooms.ExistsResult = true;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(Guid.NewGuid(), "user-1", "Ana", "/unknown");

        SendMessageResult result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(SendMessageOutcomeEnum.UnknownCommandRejected, result.OutcomeEnum);
        Assert.Null(result.Message);
        Assert.Null(result.StockCode);
        Assert.Equal(0, _messages.AddAsyncCallCount);
        Assert.Equal(0, _publisher.PublishCallCount);
    }

    // Behavior 23
    [Fact]
    public async Task Handle_RoomDoesNotExist_PlainContent_ThrowsRoomNotFoundException()
    {
        Guid roomId = Guid.NewGuid();
        _rooms.ExistsResult = false;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(roomId, "user-1", "Ana", "hello");

        RoomNotFoundException ex = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(roomId, ex.RoomId);
        Assert.Equal(0, _messages.AddAsyncCallCount);
        Assert.Equal(0, _publisher.PublishCallCount);
    }

    // Behavior 23
    [Fact]
    public async Task Handle_RoomDoesNotExist_StockCommand_ThrowsBeforeParse()
    {
        Guid roomId = Guid.NewGuid();
        _rooms.ExistsResult = false;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(roomId, "user-1", "Ana", "/stock=aapl.us");

        RoomNotFoundException ex = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(roomId, ex.RoomId);
        Assert.Equal(0, _publisher.PublishCallCount);
        Assert.Equal(0, _messages.AddAsyncCallCount);
    }

    // Behavior 23
    [Fact]
    public async Task Handle_EmptyRoomId_TreatedAsUnknownRoom_ThrowsRoomNotFoundException()
    {
        _rooms.ExistsResult = false; // contract: ExistsAsync(Guid.Empty) is false
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(Guid.Empty, "user-1", "Ana", "hello");

        RoomNotFoundException ex = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(Guid.Empty, ex.RoomId);
    }

    // Behavior 24
    [Theory]
    [InlineData("", "Ana", "hello")]        // empty AuthorUserId
    [InlineData("user-1", "Ana", "")]        // empty Content
    [InlineData("user-1", "Ana", "   ")]     // whitespace Content
    public async Task Handle_InvalidFieldsWithExistingRoom_ThrowsDomainException(
        string authorUserId, string displayName, string content)
    {
        _rooms.ExistsResult = true;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(Guid.NewGuid(), authorUserId, displayName, content);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, _messages.AddAsyncCallCount);
    }

    // Behavior 24
    [Fact]
    public async Task Handle_Content1001Chars_ThrowsDomainException()
    {
        _rooms.ExistsResult = true;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(Guid.NewGuid(), "user-1", "Ana", new string('x', 1001));

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, _messages.AddAsyncCallCount);
    }

    // Behavior 24
    [Fact]
    public async Task Handle_DisplayName101Chars_ThrowsDomainException()
    {
        _rooms.ExistsResult = true;
        SendMessageCommandHandler handler = CreateHandler();
        SendMessageCommand command = new SendMessageCommand(Guid.NewGuid(), "user-1", new string('a', 101), "hello");

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, _messages.AddAsyncCallCount);
    }
}
