using System.Globalization;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Bot.Stock;

// Parses stooq's quote CSV (Symbol,Date,Time,Open,High,Low,Close,Volume; Close is index 6).
// Vendor-specific, so it stays in the Bot; returns null when the symbol is unknown (Close = N/D)
// or the body is malformed, yielding a Domain StockQuote otherwise.
public static class StooqCsvParser
{
    private const int CloseColumnIndex = 6;
    private const int MinimumColumnCount = 7;
    private const string NotAvailable = "N/D";

    public static StockQuote? Parse(string csv)
    {
        IReadOnlyList<string> dataFields = ExtractDataRowFields(csv);

        return dataFields.Count < MinimumColumnCount
            ? null
            : BuildQuote(dataFields);
    }

    private static StockQuote? BuildQuote(IReadOnlyList<string> fields)
    {
        string symbol = fields[0].Trim();
        string close = fields[CloseColumnIndex].Trim();
        bool notAvailable = string.Equals(close, NotAvailable, StringComparison.OrdinalIgnoreCase);
        bool parsed = decimal.TryParse(
            close, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal closeValue);

        return notAvailable || !parsed
            ? null
            : new StockQuote(symbol, closeValue);
    }

    // Skip the header, tolerate CRLF / blank lines / surrounding whitespace, return the first data row's fields.
    private static IReadOnlyList<string> ExtractDataRowFields(string csv)
    {
        string[] lines = csv.Split('\n');
        IEnumerable<string> nonBlankLines =
            from string line in lines
            let trimmed = line.Trim()
            where trimmed.Length > 0
            select trimmed;

        string? dataRow = nonBlankLines.Skip(1).FirstOrDefault();

        return dataRow is null
            ? []
            : dataRow.Split(',');
    }
}
