namespace com.jobsite.chat.Domain.Exceptions;

public sealed class RoomNotFoundException(Guid roomId)
    : Exception($"Chat room '{roomId}' was not found.")
{
    public Guid RoomId { get; } = roomId;
}
