using com.jobsite.chat.Api.Identity;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Service.Chat.GetLatestMessages;
using com.jobsite.chat.Service.Chat.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace com.jobsite.chat.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub<IChatClient>
{
    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator) => _mediator = mediator;

    public async Task JoinRoom(Guid roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroups.Room(roomId));
        IReadOnlyList<ChatMessageDto> history = await _mediator.Send(new GetLatestMessagesQuery(roomId));
        await Clients.Caller.LoadHistory(history);
    }

    public Task LeaveRoom(Guid roomId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatGroups.Room(roomId));

    public async Task SendMessage(Guid roomId, string content)
    {
        (string userId, string displayName) = Context.User!.ToChatAuthor();
        SendMessageResult result = await _mediator.Send(
            new SendMessageCommand(roomId, userId, displayName, content));
        
        switch (result.Outcome)
        {
            case SendMessageOutcome.MessagePersisted:
                await Clients.Group(ChatGroups.Room(roomId)).ReceiveMessage(result.Message!);
                break;
            case SendMessageOutcome.StockCommandQueued:
                await Clients.Caller.CommandAccepted(result.StockCode!);
                break;
            case SendMessageOutcome.UnknownCommandRejected:
                await Clients.Caller.CommandRejected("Unknown command.");
                break;
        }
    }
}
