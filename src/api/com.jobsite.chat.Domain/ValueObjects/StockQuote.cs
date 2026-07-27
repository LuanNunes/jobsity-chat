using System.Globalization;

namespace com.jobsite.chat.Domain.ValueObjects;

public sealed record StockQuote(string Symbol, decimal Close)
{

    public string ToQuoteMessage() =>
        $"{Symbol.ToUpperInvariant()} quote is ${Close.ToString("0.00", CultureInfo.InvariantCulture)} per share";

    public static string NotFoundMessage(string stockCode) =>
        $"{stockCode} is not a valid stock symbol.";
}
