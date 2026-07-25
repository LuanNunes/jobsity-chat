using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Domain.Dtos;

// plain output record mapped from a valid ChatRoom.
public sealed record ChatRoomDto(Guid Id, string Name)
{
    public static ChatRoomDto FromEntity(ChatRoom room) => new(room.Id, room.Name);
}
