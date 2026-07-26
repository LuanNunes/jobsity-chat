using com.jobsite.chat.Api.Infrastructure.Identity;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Service.Chat.Commands;
using com.jobsite.chat.Service.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace com.jobsite.chat.Api.Features.Chat;

[Authorize]
public sealed class ChatHub(IMediator mediator) : Hub<IChatClient>
{
    public async Task JoinRoom(Guid roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroups.Room(roomId));
        IReadOnlyList<ChatMessageDto> history = await mediator.Send(new GetLatestMessagesQuery(roomId));
        await Clients.Caller.LoadHistory(history);
    }

    public Task LeaveRoom(Guid roomId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatGroups.Room(roomId));

    public async Task SendMessage(Guid roomId, string content)
    {
        (string userId, string displayName) = Context.User!.ToChatAuthor();
        SendMessageResult result = await mediator.Send(new SendMessageCommand(roomId, userId, displayName, content));

        Dictionary<SendMessageOutcomeEnum, Func<Task>> handlers = new()
        {
            [SendMessageOutcomeEnum.MessagePersisted] = () => Clients.Group(ChatGroups.Room(roomId)).ReceiveMessage(result.Message!),
            [SendMessageOutcomeEnum.StockCommandQueued] = () => Clients.Caller.CommandAccepted(result.StockCode!),
            [SendMessageOutcomeEnum.UnknownCommandRejected] = () => Clients.Caller.CommandRejected("Unknown command."),
        };

        await handlers[result.OutcomeEnum]();
    }
}
