using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Domain.ValueObjects;

public class StockQuoteTests
{

    [Fact]
    public void ToQuoteMessage_LowercaseSymbol_UppercasesAndFormatsTwoDecimals()
    {
        string text = new StockQuote("aapl.us", 93.42m).ToQuoteMessage();

        Assert.Equal("AAPL.US quote is $93.42 per share", text);
    }

    [Fact]
    public void ToQuoteMessage_SingleDecimalValue_PadsToTwoDecimals()
    {
        string text = new StockQuote("AAPL.US", 93.4m).ToQuoteMessage();

        Assert.Equal("AAPL.US quote is $93.40 per share", text);
    }

    [Fact]
    public void ToQuoteMessage_HalfDecimalValue_RendersTwoDecimals()
    {
        string text = new StockQuote("AAPL.US", 93.5m).ToQuoteMessage();

        Assert.Equal("AAPL.US quote is $93.50 per share", text);
    }

    [Fact]
    public void NotFoundMessage_RequestedCode_ReturnsInvalidSymbolLineVerbatim()
    {
        string text = StockQuote.NotFoundMessage("aapl.us");

        Assert.Equal("aapl.us is not a valid stock symbol.", text);
    }
}
