using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Domain.Dtos;

public sealed record ChatRoomDto(Guid Id, string Name)
{
    public static ChatRoomDto FromEntity(ChatRoom room) => new(room.Id, room.Name);
}
