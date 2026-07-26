using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Domain.ValueObjects;

// StockQuote rich value object: ToQuoteMessage (uppercase symbol, 2-decimal invariant) + static NotFoundMessage.
public class StockQuoteTests
{
    // ToQuoteMessage uppercases the symbol and formats close to 2 decimals with an invariant '$' and '.'.
    [Fact]
    public void ToQuoteMessage_LowercaseSymbol_UppercasesAndFormatsTwoDecimals()
    {
        string text = new StockQuote("aapl.us", 93.42m).ToQuoteMessage();

        Assert.Equal("AAPL.US quote is $93.42 per share", text);
    }

    // ToQuoteMessage pads a single-decimal value to two decimals (93.4 -> "93.40").
    [Fact]
    public void ToQuoteMessage_SingleDecimalValue_PadsToTwoDecimals()
    {
        string text = new StockQuote("AAPL.US", 93.4m).ToQuoteMessage();

        Assert.Equal("AAPL.US quote is $93.40 per share", text);
    }

    // ToQuoteMessage renders a .5 value with two decimals (93.5 -> "93.50").
    [Fact]
    public void ToQuoteMessage_HalfDecimalValue_RendersTwoDecimals()
    {
        string text = new StockQuote("AAPL.US", 93.5m).ToQuoteMessage();

        Assert.Equal("AAPL.US quote is $93.50 per share", text);
    }

    // NotFoundMessage echoes the requested code verbatim (not uppercased) in the invalid-symbol line.
    [Fact]
    public void NotFoundMessage_RequestedCode_ReturnsInvalidSymbolLineVerbatim()
    {
        string text = StockQuote.NotFoundMessage("aapl.us");

        Assert.Equal("aapl.us is not a valid stock symbol.", text);
    }
}
