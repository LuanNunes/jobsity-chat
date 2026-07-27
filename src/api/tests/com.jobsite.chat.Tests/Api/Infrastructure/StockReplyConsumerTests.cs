using System.Text.Json;
using com.jobsite.chat.Api.Features.Chat;
using com.jobsite.chat.Api.Infrastructure;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Service.Chat.Commands;
using com.jobsite.chat.Shared.Messaging;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace com.jobsite.chat.Tests.Api.Infrastructure;

public class StockReplyConsumerTests
{

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IHubContext<ChatHub, IChatClient> _hub = Substitute.For<IHubContext<ChatHub, IChatClient>>();
    private readonly IHubClients<IChatClient> _clients = Substitute.For<IHubClients<IChatClient>>();
    private readonly IChatClient _groupClient = Substitute.For<IChatClient>();

    public StockReplyConsumerTests()
    {
        _hub.Clients.Returns(_clients);
        _clients.Group(Arg.Any<string>()).Returns(_groupClient);
    }

    private static byte[] Serialize(StockQuoteReply reply)
        => JsonSerializer.SerializeToUtf8Bytes(reply, JsonOpts);

    [Fact]
    public async Task HandleReplyAsync_ReplyBody_DispatchesPostBotMessageCommandWithRoomAndText()
    {
        Guid roomId = Guid.NewGuid();
        const string botText = "AAPL.US quote is $93.42 per share";
        StockQuoteReply reply = new StockQuoteReply(roomId, botText, true);
        ChatMessageDto dto = new ChatMessageDto(
            Guid.NewGuid(), roomId, "Bot", botText, true, DateTimeOffset.UtcNow);
        _mediator.Send(Arg.Any<PostBotMessageCommand>(), Arg.Any<CancellationToken>()).Returns(dto);
        StockReplyConsumer consumer = new StockReplyConsumer(_hub);

        await consumer.HandleReplyAsync(_mediator, Serialize(reply), CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<PostBotMessageCommand>(command =>
                command != null
                && command.RoomId == roomId
                && command.Content == botText),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleReplyAsync_ReplyBody_BroadcastsReturnedDtoToRoomGroup()
    {
        Guid roomId = Guid.NewGuid();
        const string botText = "AAPL.US quote is $93.42 per share";
        StockQuoteReply reply = new StockQuoteReply(roomId, botText, true);
        ChatMessageDto dto = new ChatMessageDto(
            Guid.NewGuid(), roomId, "Bot", botText, true, DateTimeOffset.UtcNow);
        _mediator.Send(Arg.Any<PostBotMessageCommand>(), Arg.Any<CancellationToken>()).Returns(dto);
        StockReplyConsumer consumer = new StockReplyConsumer(_hub);

        await consumer.HandleReplyAsync(_mediator, Serialize(reply), CancellationToken.None);

        _clients.Received(1).Group($"room:{roomId}");
        await _groupClient.Received(1).ReceiveMessage(dto);
    }

    [Fact]
    public async Task HandleReplyAsync_UnsuccessfulReply_StillPostsAndBroadcastsBotText()
    {
        Guid roomId = Guid.NewGuid();
        const string botText = "aapl.us is not a valid stock symbol.";
        StockQuoteReply reply = new StockQuoteReply(roomId, botText, false);
        ChatMessageDto dto = new ChatMessageDto(
            Guid.NewGuid(), roomId, "Bot", botText, true, DateTimeOffset.UtcNow);
        _mediator.Send(Arg.Any<PostBotMessageCommand>(), Arg.Any<CancellationToken>()).Returns(dto);
        StockReplyConsumer consumer = new StockReplyConsumer(_hub);

        await consumer.HandleReplyAsync(_mediator, Serialize(reply), CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<PostBotMessageCommand>(command =>
                command != null && command.RoomId == roomId && command.Content == botText),
            Arg.Any<CancellationToken>());
        _clients.Received(1).Group($"room:{roomId}");
        await _groupClient.Received(1).ReceiveMessage(dto);
    }
}
