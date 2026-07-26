using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Tests.Domain.ValueObjects;

// Behavior 13: StockSymbol.TryCreate.
public class StockSymbolTests
{
    [Fact]
    public void TryCreate_ValidRawUppercase_ReturnsTrueAndNormalizesLowercase()
    {
        bool ok = StockSymbol.TryCreate("AAPL.US", out StockSymbol? symbol);
        Assert.True(ok);
        Assert.NotNull(symbol);
        Assert.Equal("aapl.us", symbol!.Value);
    }

    [Fact]
    public void TryCreate_ValidRawWithSurroundingWhitespace_TrimsAndNormalizes()
    {
        bool ok = StockSymbol.TryCreate("  AAPL.US  ", out StockSymbol? symbol);
        Assert.True(ok);
        Assert.NotNull(symbol);
        Assert.Equal("aapl.us", symbol!.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a b")]
    public void TryCreate_InvalidRaw_ReturnsFalseAndNullSymbol(string raw)
    {
        bool ok = StockSymbol.TryCreate(raw, out StockSymbol? symbol);
        Assert.False(ok);
        Assert.Null(symbol);
    }

    [Fact]
    public void TryCreate_TwentyOneChars_ReturnsFalse()
    {
        bool ok = StockSymbol.TryCreate(new string('a', 21), out StockSymbol? symbol);
        Assert.False(ok);
        Assert.Null(symbol);
    }

    [Fact]
    public void TryCreate_TwentyChars_ReturnsTrue()
    {
        bool ok = StockSymbol.TryCreate(new string('a', 20), out StockSymbol? symbol);
        Assert.True(ok);
        Assert.NotNull(symbol);
        Assert.Equal(new string('a', 20), symbol!.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        StockSymbol.TryCreate("aapl.us", out StockSymbol? symbol);
        Assert.NotNull(symbol);
        Assert.Equal("aapl.us", symbol!.ToString());
    }
}
