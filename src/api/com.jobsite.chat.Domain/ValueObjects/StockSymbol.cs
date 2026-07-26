using System.Text.RegularExpressions;

namespace com.jobsite.chat.Domain.ValueObjects;

// Normalized stock symbol value object (e.g. "aapl.us"). Non-throwing creation:
// an invalid symbol is an expected parse outcome, not a violated invariant.
public sealed partial record StockSymbol
{
    private const int MaxLength = 20;

    private StockSymbol(string value) => Value = value;

    public string Value { get; }  // normalized lowercase, e.g. "aapl.us"

    public static bool TryCreate(string? raw, out StockSymbol? symbol)
    {
        symbol = null;
        if (raw is null)
        {
            return false;
        }

        string normalized = raw.Trim().ToLowerInvariant();
        if (normalized.Length is < 1 or > MaxLength || !SymbolPattern().IsMatch(normalized))
        {
            return false;
        }

        symbol = new StockSymbol(normalized);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9.^\-]+$")]
    private static partial Regex SymbolPattern();
}
