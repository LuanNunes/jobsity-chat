namespace com.jobsite.chat.Domain.Dtos;

public sealed record StockQuoteRequestDto(Guid RoomId, string StockCode);
