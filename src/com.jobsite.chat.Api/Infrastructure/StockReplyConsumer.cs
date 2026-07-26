using System.Text.Json;
using com.jobsite.chat.Api.Hubs;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Service.Chat.PostBotMessage;
using com.jobsite.chat.Service.Exceptions;
using com.jobsite.chat.Shared.Abstractions;
using com.jobsite.chat.Shared.Messaging;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace com.jobsite.chat.Api.Infrastructure;

// Consumes stock-quote replies, persists them as bot messages, and broadcasts to the room.
// Singleton BackgroundService: resolves scoped IMediator from a per-message scope; IHubContext is singleton-safe.
internal sealed class StockReplyConsumer : RabbitMqQueueConsumer
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IHubContext<ChatHub, IChatClient> _hub;

    public StockReplyConsumer(
        IRabbitMqConnection connection,
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        IHubContext<ChatHub, IChatClient> hub,
        ILogger<StockReplyConsumer> logger)
        : base(connection, options.Value.RepliesQueue, logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
    }

    // Test seam: HandleReplyAsync needs only the hub; the broker plumbing (base) is not exercised here.
    internal StockReplyConsumer(IHubContext<ChatHub, IChatClient> hub) : base(null!, null!, null!) => _hub = hub;

    protected override async Task ProcessAsync(ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        await using AsyncServiceScope scope = _scopeFactory!.CreateAsyncScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await HandleReplyAsync(mediator, body, ct);
        }
        catch (RoomNotFoundException exception)
        {
            // Expected: the room was deleted between the request and the reply. Drop (ack), don't error.
            Logger.LogWarning(exception, "Stock reply targeted a room that no longer exists; discarding.");
        }
    }

    // Unit-testable per-message seam: mediator is passed in (resolved from a per-message scope by ProcessAsync).
    // Deserialize the reply, dispatch PostBotMessageCommand(RoomId, BotText), broadcast the returned dto.
    internal async Task HandleReplyAsync(IMediator mediator, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        StockQuoteReply reply = JsonSerializer.Deserialize<StockQuoteReply>(body.Span, MessagingJson.Options)
            ?? throw new JsonException("Stock reply payload deserialized to null.");
        ChatMessageDto dto = await mediator.Send(new PostBotMessageCommand(reply.RoomId, reply.BotText), ct);
        await _hub.Clients.Group(ChatGroups.Room(reply.RoomId)).ReceiveMessage(dto);
    }
}
