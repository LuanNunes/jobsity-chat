using System.Globalization;

namespace com.jobsite.chat.Domain.ValueObjects;

// A resolved stock quote (symbol + closing price). Produced by an external-price adapter
// (the Bot's stooq parser) and knows how to render its own chat messages.
public sealed record StockQuote(string Symbol, decimal Close)
{
    // "AAPL.US quote is $93.42 per share"
    public string ToQuoteMessage() =>
        $"{Symbol.ToUpperInvariant()} quote is ${Close.ToString("0.00", CultureInfo.InvariantCulture)} per share";

    // "aapl.us is not a valid stock symbol." — for a code with no resolvable quote.
    public static string NotFoundMessage(string stockCode) =>
        $"{stockCode} is not a valid stock symbol.";
}
