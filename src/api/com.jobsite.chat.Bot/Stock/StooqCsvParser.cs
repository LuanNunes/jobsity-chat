using System.Globalization;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Bot.Stock;

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
