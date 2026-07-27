using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Service.Chat.Commands;
using com.jobsite.chat.Tests.Service.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace com.jobsite.chat.Tests.Service.Chat.PostBotMessage;

public class PostBotMessageCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 25, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeChatRoomRepository _rooms = new();
    private readonly FakeChatMessageRepository _messages = new();
    private readonly FakeTimeProvider _clock = new(FixedNow);

    private PostBotMessageCommandHandler CreateHandler()
        => new(_rooms, _messages, _clock);

    [Fact]
    public async Task Handle_ValidCommand_PersistsBotMessageAndReturnsDto()
    {
        Guid roomId = Guid.NewGuid();
        _rooms.ExistsResult = true;
        PostBotMessageCommandHandler handler = CreateHandler();
        PostBotMessageCommand command = new PostBotMessageCommand(roomId, "AAPL.US quote is $93.42 per share");

        ChatMessageDto dto = await handler.Handle(command, CancellationToken.None);

        ChatMessage persisted = Assert.Single(_messages.Added);
        Assert.True(persisted.Author.IsBot);
        Assert.Null(persisted.Author.UserId);
        Assert.Equal("StockBot", persisted.Author.DisplayName);

        Assert.True(dto.IsFromBot);
        Assert.Equal("AAPL.US quote is $93.42 per share", dto.Content);
        Assert.Equal(roomId, dto.RoomId);
    }

    [Fact]
    public async Task Handle_StockLikeContentFromBot_PersistedVerbatimNotParsed()
    {
        Guid roomId = Guid.NewGuid();
        _rooms.ExistsResult = true;
        PostBotMessageCommandHandler handler = CreateHandler();
        PostBotMessageCommand command = new PostBotMessageCommand(roomId, "/stock=aapl.us");

        ChatMessageDto dto = await handler.Handle(command, CancellationToken.None);

        ChatMessage persisted = Assert.Single(_messages.Added);
        Assert.Equal("/stock=aapl.us", persisted.Content);
        Assert.True(persisted.Author.IsBot);
        Assert.Equal("/stock=aapl.us", dto.Content);
    }

    [Fact]
    public async Task Handle_UnknownRoom_ThrowsRoomNotFoundException()
    {
        Guid roomId = Guid.NewGuid();
        _rooms.ExistsResult = false;
        PostBotMessageCommandHandler handler = CreateHandler();
        PostBotMessageCommand command = new PostBotMessageCommand(roomId, "some content");

        RoomNotFoundException ex = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(roomId, ex.RoomId);
        Assert.Equal(0, _messages.AddAsyncCallCount);
    }

    [Theory]
    [InlineData("", "StockBot")]
    [InlineData("   ", "StockBot")]
    public async Task Handle_EmptyOrWhitespaceContent_ThrowsDomainException(string content, string botName)
    {
        _rooms.ExistsResult = true;
        PostBotMessageCommandHandler handler = CreateHandler();
        PostBotMessageCommand command = new PostBotMessageCommand(Guid.NewGuid(), content, botName);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, _messages.AddAsyncCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyOrWhitespaceBotDisplayName_ThrowsDomainException(string botName)
    {
        _rooms.ExistsResult = true;
        PostBotMessageCommandHandler handler = CreateHandler();
        PostBotMessageCommand command = new PostBotMessageCommand(Guid.NewGuid(), "valid content", botName);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, _messages.AddAsyncCallCount);
    }
}
