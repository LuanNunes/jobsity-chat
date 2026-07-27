using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Domain.Dtos;

public sealed record NewChatMessageDto(Guid RoomId, MessageAuthor Author, string Content, DateTimeOffset SentAtUtc)
{
    public static NewChatMessageDto Create(Guid roomId, MessageAuthor author, string content, TimeProvider clock)
        => new(roomId, author, content, clock.GetUtcNow());
}
