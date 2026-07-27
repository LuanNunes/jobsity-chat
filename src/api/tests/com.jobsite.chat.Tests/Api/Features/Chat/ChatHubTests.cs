using System.Security.Claims;
using com.jobsite.chat.Api.Features.Chat;
using com.jobsite.chat.Api.Infrastructure.Identity;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Service.Chat.Commands;
using com.jobsite.chat.Service.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace com.jobsite.chat.Tests.Api.Features.Chat;

public class ChatHubTests
{
    private const string ConnectionId = "conn-123";
    private const string AuthorUserId = "user-1";
    private const string AuthorDisplayName = "Ana";

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IHubCallerClients<IChatClient> _clients = Substitute.For<IHubCallerClients<IChatClient>>();
    private readonly IChatClient _callerClient = Substitute.For<IChatClient>();
    private readonly IChatClient _groupClient = Substitute.For<IChatClient>();
    private readonly IGroupManager _groups = Substitute.For<IGroupManager>();
    private readonly HubCallerContext _context = Substitute.For<HubCallerContext>();

    public ChatHubTests()
    {
        _clients.Caller.Returns(_callerClient);
        _clients.Group(Arg.Any<string>()).Returns(_groupClient);

        ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, AuthorUserId),
                new Claim(AppClaimTypes.DisplayName, AuthorDisplayName),
            },
            authenticationType: "TestAuth"));

        _context.ConnectionId.Returns(ConnectionId);
        _context.User.Returns(principal);
    }

    private ChatHub CreateHub()
        => new ChatHub(_mediator)
        {
            Clients = _clients,
            Groups = _groups,
            Context = _context,
        };

    private static string GroupName(Guid roomId) => $"room:{roomId}";

    [Fact]
    public async Task SendMessage_MessagePersisted_BroadcastsDtoToRoomGroupOnly()
    {
        Guid roomId = Guid.NewGuid();
        ChatMessageDto dto = new ChatMessageDto(
            Guid.NewGuid(), roomId, AuthorDisplayName, "hello", false, DateTimeOffset.UtcNow);
        SendMessageResult result = new SendMessageResult(SendMessageOutcomeEnum.MessagePersisted, Message: dto);
        _mediator.Send(Arg.Any<SendMessageCommand>(), Arg.Any<CancellationToken>()).Returns(result);
        ChatHub hub = CreateHub();

        await hub.SendMessage(roomId, "hello");

        _clients.Received(1).Group(GroupName(roomId));
        await _groupClient.Received(1).ReceiveMessage(dto);
        await _callerClient.DidNotReceive().ReceiveMessage(Arg.Any<ChatMessageDto>());
        await _callerClient.DidNotReceive().CommandAccepted(Arg.Any<string>());
        await _callerClient.DidNotReceive().CommandRejected(Arg.Any<string>());
    }

    [Fact]
    public async Task SendMessage_StockCommandQueued_AcksCallerNoBroadcast()
    {
        Guid roomId = Guid.NewGuid();
        SendMessageResult result = new SendMessageResult(SendMessageOutcomeEnum.StockCommandQueued, StockCode: "aapl.us");
        _mediator.Send(Arg.Any<SendMessageCommand>(), Arg.Any<CancellationToken>()).Returns(result);
        ChatHub hub = CreateHub();

        await hub.SendMessage(roomId, "/stock=aapl.us");

        await _callerClient.Received(1).CommandAccepted("aapl.us");
        await _groupClient.DidNotReceive().ReceiveMessage(Arg.Any<ChatMessageDto>());
        _clients.DidNotReceive().Group(Arg.Any<string>());
        await _callerClient.DidNotReceive().CommandRejected(Arg.Any<string>());
    }

    [Fact]
    public async Task SendMessage_UnknownCommandRejected_RejectsCallerNoBroadcast()
    {
        Guid roomId = Guid.NewGuid();
        SendMessageResult result = new SendMessageResult(SendMessageOutcomeEnum.UnknownCommandRejected);
        _mediator.Send(Arg.Any<SendMessageCommand>(), Arg.Any<CancellationToken>()).Returns(result);
        ChatHub hub = CreateHub();

        await hub.SendMessage(roomId, "/unknown");

        await _callerClient.Received(1).CommandRejected(Arg.Any<string>());
        await _groupClient.DidNotReceive().ReceiveMessage(Arg.Any<ChatMessageDto>());
        _clients.DidNotReceive().Group(Arg.Any<string>());
        await _callerClient.DidNotReceive().CommandAccepted(Arg.Any<string>());
    }

    [Fact]
    public async Task SendMessage_BuildsCommandWithAuthorFromContextUser()
    {
        Guid roomId = Guid.NewGuid();
        SendMessageResult result = new SendMessageResult(SendMessageOutcomeEnum.UnknownCommandRejected);
        _mediator.Send(Arg.Any<SendMessageCommand>(), Arg.Any<CancellationToken>()).Returns(result);
        ChatHub hub = CreateHub();

        await hub.SendMessage(roomId, "hello world");

        await _mediator.Received(1).Send(
            Arg.Is<SendMessageCommand>(command =>
                command != null
                && command.RoomId == roomId
                && command.AuthorUserId == AuthorUserId
                && command.AuthorDisplayName == AuthorDisplayName
                && command.Content == "hello world"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinRoom_AddsToGroupAndLoadsHistoryToCaller()
    {
        Guid roomId = Guid.NewGuid();
        ChatMessageDto older = new ChatMessageDto(
            Guid.NewGuid(), roomId, "Ana", "first", false, new DateTimeOffset(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        ChatMessageDto newer = new ChatMessageDto(
            Guid.NewGuid(), roomId, "Bob", "second", false, new DateTimeOffset(2026, 7, 25, 9, 1, 0, TimeSpan.Zero));
        IReadOnlyList<ChatMessageDto> history = new List<ChatMessageDto> { older, newer };
        _mediator.Send(Arg.Any<GetLatestMessagesQuery>(), Arg.Any<CancellationToken>()).Returns(history);
        ChatHub hub = CreateHub();

        await hub.JoinRoom(roomId);

        await _groups.Received(1).AddToGroupAsync(ConnectionId, GroupName(roomId), Arg.Any<CancellationToken>());
        await _callerClient.Received(1).LoadHistory(Arg.Is<IReadOnlyList<ChatMessageDto>>(list =>
            list != null && list.Count == 2 && list[0] == older && list[1] == newer));
    }

    [Fact]
    public async Task JoinRoom_QueriesLatestMessagesForRoom()
    {
        Guid roomId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetLatestMessagesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChatMessageDto>());
        ChatHub hub = CreateHub();

        await hub.JoinRoom(roomId);

        await _mediator.Received(1).Send(
            Arg.Is<GetLatestMessagesQuery>(query => query != null && query.RoomId == roomId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinRoom_UsesRoomColonGuidGroupNameFormat()
    {
        Guid roomId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetLatestMessagesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChatMessageDto>());
        ChatHub hub = CreateHub();

        await hub.JoinRoom(roomId);

        await _groups.Received(1).AddToGroupAsync(
            Arg.Any<string>(), $"room:{roomId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveRoom_RemovesFromGroup()
    {
        Guid roomId = Guid.NewGuid();
        ChatHub hub = CreateHub();

        await hub.LeaveRoom(roomId);

        await _groups.Received(1).RemoveFromGroupAsync(
            ConnectionId, GroupName(roomId), Arg.Any<CancellationToken>());
    }
}
