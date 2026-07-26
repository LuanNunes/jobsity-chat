using com.jobsite.chat.Shared.Contracts;

namespace com.jobsite.chat.Bot.Stock;

// Typed client for stooq's quote CSV endpoint: GET q/l/?s={code}&f=sd2t2ohlcv&h&e=csv (base https://stooq.com/).
internal sealed class StooqStockPriceClient(HttpClient httpClient) : IStockPriceClient
{
    public async Task<string> GetQuoteCsvAsync(string stockCode, CancellationToken ct)
    {
        string requestUri = $"q/l/?s={Uri.EscapeDataString(stockCode)}&f=sd2t2ohlcv&h&e=csv";
        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
