namespace com.jobsite.chat.Shared.Messaging;

// Reply contract: BotText is the fully-formatted line posted verbatim (success OR invalid-symbol).
// Success is signal/logging only; Api posts BotText either way.
public sealed record StockQuoteReply(Guid RoomId, string BotText, bool Success);
