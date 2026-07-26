namespace com.jobsite.chat.Domain.Exceptions;

// raised when a command targets a non-existent room.
public sealed class RoomNotFoundException(Guid roomId)
    : Exception($"Chat room '{roomId}' was not found.")
{
    public Guid RoomId { get; } = roomId;
}
