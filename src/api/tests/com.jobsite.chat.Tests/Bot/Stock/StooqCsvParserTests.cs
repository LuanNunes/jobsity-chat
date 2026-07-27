extern alias BotAssembly;
using System.Globalization;
using BotAssembly::com.jobsite.chat.Bot.Stock;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Bot.Stock;

public class StooqCsvParserTests
{
    private const string Header = "Symbol,Date,Time,Open,High,Low,Close,Volume";

    [Fact]
    public void Parse_ValidCsvWithHeaderAndDataRow_ReturnsQuoteWithSymbolAndClose()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,93.10,93.90,92.80,93.42,12345";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal("AAPL.US", quote!.Symbol);
        Assert.Equal(93.42m, quote.Close);
    }

    [Fact]
    public void Parse_ValidRow_ReadsCloseFromIndexSix()
    {

        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,93.42,12345";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal(93.42m, quote!.Close);
    }

    [Fact]
    public void Parse_CloseIsND_ReturnsNull()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,N/D,N/D,N/D,N/D,0";

        Assert.Null(StooqCsvParser.Parse(csv));
    }

    [Theory]
    [InlineData("n/d")]
    [InlineData("N/d")]
    [InlineData("n/D")]
    public void Parse_CloseIsNDCaseInsensitive_ReturnsNull(string nd)
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00," + nd + ",0";

        Assert.Null(StooqCsvParser.Parse(csv));
    }

    [Fact]
    public void Parse_RowWithFewerThanSevenColumns_ReturnsNull()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00";

        Assert.Null(StooqCsvParser.Parse(csv));
    }

    [Fact]
    public void Parse_HeaderOnly_ReturnsNull() => Assert.Null(StooqCsvParser.Parse(Header));

    [Fact]
    public void Parse_EmptyString_ReturnsNull() => Assert.Null(StooqCsvParser.Parse(string.Empty));

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsNull() => Assert.Null(StooqCsvParser.Parse("   \t  "));

    [Fact]
    public void Parse_BlankLinesAroundDataRow_ReturnsQuote()
    {
        string csv = "\n" + Header + "\n\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,93.42,12345" + "\n\n";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal(93.42m, quote!.Close);
    }

    [Fact]
    public void Parse_CrlfLineEndings_ReturnsQuote()
    {
        string csv = Header + "\r\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,93.42,12345" + "\r\n";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal("AAPL.US", quote!.Symbol);
        Assert.Equal(93.42m, quote.Close);
    }

    [Fact]
    public void Parse_InvariantCultureDecimal_DotParsesToExactValue()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,93.42,12345";

        StockQuote? quote = StooqCsvParser.Parse(csv);

        Assert.NotNull(quote);
        Assert.Equal(decimal.Parse("93.42", CultureInfo.InvariantCulture), quote!.Close);
    }

    [Fact]
    public void Parse_NonNumericClose_ReturnsNull()
    {
        string csv = Header + "\n" + "AAPL.US,2026-07-25,22:00:04,1.00,2.00,3.00,abc,12345";

        Assert.Null(StooqCsvParser.Parse(csv));
    }
}
