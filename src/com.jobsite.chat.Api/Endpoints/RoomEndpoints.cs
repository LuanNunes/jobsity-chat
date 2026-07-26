using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Service.Chat.Rooms;
using MediatR;

namespace com.jobsite.chat.Api.Endpoints;

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

    private static async Task<IResult> CreateRoomAsync(CreateRoomRequest request, IMediator mediator)
    {
        ChatRoomDto room = await mediator.Send(new CreateRoomCommand(request.Name));
        return Results.Created($"/api/rooms/{room.Id}", room);
    }
}
