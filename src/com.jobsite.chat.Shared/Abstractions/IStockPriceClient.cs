namespace com.jobsite.chat.Shared.Abstractions;

// Typed HTTP client for the stooq quote CSV endpoint. Returns the raw CSV body; parsing lives in the Bot.
public interface IStockPriceClient
{
    Task<string> GetQuoteCsvAsync(string stockCode, CancellationToken ct);
}
