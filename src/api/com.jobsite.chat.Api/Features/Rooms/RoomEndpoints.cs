using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Api.Exceptions;
using com.jobsite.chat.Api.Features.Chat;
using com.jobsite.chat.Service.Chat.Commands;
using com.jobsite.chat.Service.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace com.jobsite.chat.Api.Features.Rooms;

// JSON rooms surface (spec §2.4). The whole group requires auth; DomainException from the room-name
// factory is converted to 400 problem+json by the group's DomainExceptionEndpointFilter.
public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/rooms");
        group.RequireAuthorization();
        group.AddEndpointFilter<DomainExceptionEndpointFilter>();

        group.MapGet("/", GetRoomsAsync);
        group.MapPost("/", CreateRoomAsync);

        return app;
    }

    private static async Task<IResult> GetRoomsAsync(IMediator mediator)
    {
        IReadOnlyList<ChatRoomDto> rooms = await mediator.Send(new GetRoomsQuery());
        return Results.Ok(rooms);
    }

    private static async Task<IResult> CreateRoomAsync(
        CreateRoomCommand command,
        IMediator mediator,
        IHubContext<ChatHub, IChatClient> hub,
        ILoggerFactory loggerFactory)
    {
        ChatRoomDto room = await mediator.Send(command);

        // The room is already persisted; a broadcast failure must not fail the create.
        try
        {
            await hub.Clients.All.RoomCreated(room);
        }
        catch (Exception exception)
        {
            ILogger logger = loggerFactory.CreateLogger("RoomEndpoints");
            logger.LogWarning(exception, "Failed to broadcast RoomCreated for room {RoomId}.", room.Id);
        }

        return Results.Created($"/api/rooms/{room.Id}", room);
    }
}
