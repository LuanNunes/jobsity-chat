extern alias BotAssembly;
using System.Globalization;
using BotAssembly::com.jobsite.chat.Bot.Stock;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Bot.Stock;

// StooqCsvParser.Parse -> StockQuote? (null = not found). Columns Symbol,Date,Time,Open,High,Low,Close,Volume (Close index 6).
public class StooqCsvParserTests
{
    private const string Header = "Symbol,Date,Time,Open,High,Low,Close,Volume";

    // Valid CSV (header + data row) -> quote with Symbol "AAPL.US", Close 93.42m.
    [Fact]
    public void Parse_ValidCsvWithHeaderAndDataRow_ReturnsQuoteWithSymbolAndClose()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,93.10,93.90,92.80,93.42,12345";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal("AAPL.US", quote!.Symbol);
        Assert.Equal(93.42m, quote.Close);
    }

    // Close column is read from index 6 (not another numeric column).
    [Fact]
    public void Parse_ValidRow_ReadsCloseFromIndexSix()
    {
        // Open=1, High=2, Low=3, Close=93.42 -> must pick Close, not Open/High/Low.
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,93.42,12345";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal(93.42m, quote!.Close);
    }

    // Close "N/D" -> null.
    [Fact]
    public void Parse_CloseIsND_ReturnsNull()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,N/D,N/D,N/D,N/D,0";

        Assert.Null(StooqCsvParser.Parse(csv));
    }

    // Close "N/D" case-insensitive -> null.
    [Theory]
    [InlineData("n/d")]
    [InlineData("N/d")]
    [InlineData("n/D")]
    public void Parse_CloseIsNDCaseInsensitive_ReturnsNull(string nd)
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00," + nd + ",0";

        Assert.Null(StooqCsvParser.Parse(csv));
    }

    // Row with fewer than 7 columns -> null (no throw).
    [Fact]
    public void Parse_RowWithFewerThanSevenColumns_ReturnsNull()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00"; // 6 fields, no Close index

        Assert.Null(StooqCsvParser.Parse(csv));
    }

    // Header-only (no data row) -> null (no throw).
    [Fact]
    public void Parse_HeaderOnly_ReturnsNull() => Assert.Null(StooqCsvParser.Parse(Header));

    // Empty string -> null (no throw).
    [Fact]
    public void Parse_EmptyString_ReturnsNull() => Assert.Null(StooqCsvParser.Parse(string.Empty));

    // Whitespace-only -> null (no throw).
    [Fact]
    public void Parse_WhitespaceOnly_ReturnsNull() => Assert.Null(StooqCsvParser.Parse("   \t  "));

    // Blank lines around the data row are tolerated -> quote.
    [Fact]
    public void Parse_BlankLinesAroundDataRow_ReturnsQuote()
    {
        string csv = "\n" + Header + "\n\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,93.42,12345" + "\n\n";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal(93.42m, quote!.Close);
    }

    // CRLF line endings are tolerated -> quote (no stray '\r' corrupting the parsed value).
    [Fact]
    public void Parse_CrlfLineEndings_ReturnsQuote()
    {
        string csv = Header + "\r\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,93.42,12345" + "\r\n";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal("AAPL.US", quote!.Symbol);
        Assert.Equal(93.42m, quote.Close);
    }

    // Decimal parsed with InvariantCulture: "93.42" (dot) parses to 93.42m.
    [Fact]
    public void Parse_InvariantCultureDecimal_DotParsesToExactValue()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,93.42,12345";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal(decimal.Parse("93.42", CultureInfo.InvariantCulture), quote!.Close);
    }

    // Non-numeric Close that is not "N/D" -> null (TryParse fails, no throw).
    [Fact]
    public void Parse_NonNumericClose_ReturnsNull()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,abc,12345";

        Assert.Null(StooqCsvParser.Parse(csv));
    }
}
