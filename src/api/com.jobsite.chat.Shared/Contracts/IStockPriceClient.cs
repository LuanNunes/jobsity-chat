namespace com.jobsite.chat.Shared.Contracts;

public interface IStockPriceClient
{
    Task<string> GetQuoteCsvAsync(string stockCode, CancellationToken ct);
}
