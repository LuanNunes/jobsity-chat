namespace com.jobsite.chat.Domain.Dtos;

// StockCode = StockSymbol.Value (lowercase).
public sealed record StockQuoteRequestDto(Guid RoomId, string StockCode);
