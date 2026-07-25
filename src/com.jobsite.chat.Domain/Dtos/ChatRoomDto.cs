namespace com.jobsite.chat.Domain.Dtos;

// plain output record mapped from a valid ChatRoom.
public sealed record ChatRoomDto(Guid Id, string Name);
