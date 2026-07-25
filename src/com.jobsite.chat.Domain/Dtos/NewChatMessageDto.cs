using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Domain.Dtos;

// Input DTO for ChatMessage.Create (rev. 3.1): named, reorder-proof construction.
// Distinct from the output ChatMessageDto — do not merge.
public sealed record NewChatMessageDto(Guid RoomId, MessageAuthor Author, string Content, DateTimeOffset SentAtUtc)
{
    public static NewChatMessageDto Create(Guid roomId, MessageAuthor author, string content, TimeProvider clock)
        => new(roomId, author, content, clock.GetUtcNow());
}
