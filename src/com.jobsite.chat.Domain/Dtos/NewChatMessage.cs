using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Domain.Dtos;

// Input DTO for ChatMessage.Create (rev. 3.1): named, reorder-proof construction.
// Distinct from the output ChatMessageDto — do not merge.
public sealed record NewChatMessage(Guid RoomId, MessageAuthor Author, string Content, DateTimeOffset SentAtUtc);
