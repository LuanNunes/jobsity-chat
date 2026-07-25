namespace com.jobsite.chat.Domain.Dtos;

// plain output record mapped from a valid ChatMessage.
public sealed record ChatMessageDto(
    Guid Id,
    Guid RoomId,
    string AuthorDisplayName,
    string Content,
    bool IsFromBot,
    DateTimeOffset SentAtUtc);
