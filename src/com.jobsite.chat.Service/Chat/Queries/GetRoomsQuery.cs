using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Shared.Contracts.Repositories;
using MediatR;

namespace com.jobsite.chat.Service.Chat.Queries;

public sealed record GetRoomsQuery() : IRequest<IReadOnlyList<ChatRoomDto>>;

// Map all rooms from GetAllAsync to DTOs.
public sealed class GetRoomsQueryHandler(IChatRoomRepository rooms)
    : IRequestHandler<GetRoomsQuery, IReadOnlyList<ChatRoomDto>>
{
    public async Task<IReadOnlyList<ChatRoomDto>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatRoom> allRooms = await rooms.GetAllAsync(cancellationToken);
        return allRooms.Select(ChatRoomDto.FromEntity).ToList();
    }
}
