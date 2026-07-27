using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Domain.Rules;

namespace com.jobsite.chat.Tests.Domain.Rules;

public class ChatCommandParserTests
{

    [Fact]
    public void Parse_StockCommand_ReturnsStockCommandWithSymbol()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("/stock=aapl.us");
        Assert.Equal(ChatInputKindEnum.StockCommand, result.KindEnum);
        Assert.NotNull(result.Symbol);
        Assert.Equal("aapl.us", result.Symbol!.Value);
    }

    [Fact]
    public void Parse_UpperCasePrefixAndSymbol_NormalizesToLowercaseStockCommand()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("/STOCK=AAPL.US");
        Assert.Equal(ChatInputKindEnum.StockCommand, result.KindEnum);
        Assert.NotNull(result.Symbol);
        Assert.Equal("aapl.us", result.Symbol!.Value);
    }

    [Fact]
    public void Parse_LeadingAndTrailingWhitespace_TrimsAndReturnsStockCommand()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("  /stock=aapl.us  ");
        Assert.Equal(ChatInputKindEnum.StockCommand, result.KindEnum);
        Assert.NotNull(result.Symbol);
        Assert.Equal("aapl.us", result.Symbol!.Value);
    }

    [Theory]
    [InlineData("/stock=")]
    [InlineData("/stock")]
    public void Parse_StockPrefixWithoutValidSymbol_ReturnsUnknownCommand(string input)
    {
        ChatCommandParseResult result = ChatCommandParser.Parse(input);
        Assert.Equal(ChatInputKindEnum.UnknownCommand, result.KindEnum);
        Assert.Null(result.Symbol);
    }

    [Theory]
    [InlineData("/stock=aapl us")]
    [InlineData("/stock=abcdefghijklmnopqrstu")]
    public void Parse_StockCommandWithInvalidSymbol_ReturnsUnknownCommand(string input)
    {
        ChatCommandParseResult result = ChatCommandParser.Parse(input);
        Assert.Equal(ChatInputKindEnum.UnknownCommand, result.KindEnum);
        Assert.Null(result.Symbol);
    }

    [Fact]
    public void Parse_UnknownSlashCommand_ReturnsUnknownCommand()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("/foo");
        Assert.Equal(ChatInputKindEnum.UnknownCommand, result.KindEnum);
        Assert.Null(result.Symbol);
    }

    [Fact]
    public void Parse_PlainText_ReturnsPlainMessage()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("hello world");
        Assert.Equal(ChatInputKindEnum.PlainMessage, result.KindEnum);
        Assert.Null(result.Symbol);
    }

    [Fact]
    public void Parse_SlashNotFirstChar_ReturnsPlainMessage()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("see /stock=aapl.us");
        Assert.Equal(ChatInputKindEnum.PlainMessage, result.KindEnum);
        Assert.Null(result.Symbol);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrBlank_ReturnsPlainMessage(string? input)
    {
        ChatCommandParseResult result = ChatCommandParser.Parse(input);
        Assert.Equal(ChatInputKindEnum.PlainMessage, result.KindEnum);
        Assert.Null(result.Symbol);
    }
}
