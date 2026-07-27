using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Shared.Contracts.Repositories;
using MediatR;

namespace com.jobsite.chat.Service.Chat.Commands;

public sealed record CreateRoomCommand(string Name) : IRequest<ChatRoomDto>;

public sealed class CreateRoomCommandHandler(IChatRoomRepository rooms, TimeProvider clock)
    : IRequestHandler<CreateRoomCommand, ChatRoomDto>
{
    public async Task<ChatRoomDto> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        ChatRoom room = ChatRoom.Create(request.Name, clock.GetUtcNow());

        if (await rooms.ExistsByNameAsync(room.Name, cancellationToken))
        {
            throw new DomainException($"A room named '{room.Name}' already exists.");
        }

        await rooms.AddAsync(room, cancellationToken);

        return ChatRoomDto.FromEntity(room);
    }
}
