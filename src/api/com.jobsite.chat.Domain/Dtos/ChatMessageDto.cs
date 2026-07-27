using com.jobsite.chat.Domain.Entities;

namespace com.jobsite.chat.Domain.Dtos;

public sealed record ChatMessageDto(
    Guid Id,
    Guid RoomId,
    string AuthorDisplayName,
    string Content,
    bool IsFromBot,
    DateTimeOffset SentAtUtc)
{
    public static ChatMessageDto FromEntity(ChatMessage message)
        => new(
            message.Id,
            message.RoomId,
            message.Author.DisplayName,
            message.Content,
            message.Author.IsBot,
            message.SentAtUtc);
}
