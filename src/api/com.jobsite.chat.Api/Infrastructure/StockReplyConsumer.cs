using System.Text.Json;
using com.jobsite.chat.Api.Features.Chat;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Service.Chat.Commands;
using com.jobsite.chat.Shared.Contracts;
using com.jobsite.chat.Shared.Messaging;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace com.jobsite.chat.Api.Infrastructure;

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

            Logger.LogWarning(exception, "Stock reply targeted a room that no longer exists; discarding.");
        }
    }

    internal async Task HandleReplyAsync(IMediator mediator, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        StockQuoteReply reply = JsonSerializer.Deserialize<StockQuoteReply>(body.Span, MessagingJson.Options)
            ?? throw new JsonException("Stock reply payload deserialized to null.");
        ChatMessageDto dto = await mediator.Send(new PostBotMessageCommand(reply.RoomId, reply.BotText), ct);
        await _hub.Clients.Group(ChatGroups.Room(reply.RoomId)).ReceiveMessage(dto);
    }
}
