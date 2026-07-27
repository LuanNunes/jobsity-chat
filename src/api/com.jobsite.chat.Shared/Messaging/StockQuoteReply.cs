namespace com.jobsite.chat.Shared.Messaging;

public sealed record StockQuoteReply(Guid RoomId, string BotText, bool Success);
