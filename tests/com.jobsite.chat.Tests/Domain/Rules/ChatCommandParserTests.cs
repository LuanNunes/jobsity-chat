using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Domain.Rules;

namespace com.jobsite.chat.Tests.Domain.Rules;

// Behaviors 5–12: ChatCommandParser.Parse.
public class ChatCommandParserTests
{
    // Behavior 5
    [Fact]
    public void Parse_StockCommand_ReturnsStockCommandWithSymbol()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("/stock=aapl.us");
        Assert.Equal(ChatInputKind.StockCommand, result.Kind);
        Assert.NotNull(result.Symbol);
        Assert.Equal("aapl.us", result.Symbol!.Value);
    }

    // Behavior 6
    [Fact]
    public void Parse_UpperCasePrefixAndSymbol_NormalizesToLowercaseStockCommand()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("/STOCK=AAPL.US");
        Assert.Equal(ChatInputKind.StockCommand, result.Kind);
        Assert.NotNull(result.Symbol);
        Assert.Equal("aapl.us", result.Symbol!.Value);
    }

    // Behavior 7
    [Fact]
    public void Parse_LeadingAndTrailingWhitespace_TrimsAndReturnsStockCommand()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("  /stock=aapl.us  ");
        Assert.Equal(ChatInputKind.StockCommand, result.Kind);
        Assert.NotNull(result.Symbol);
        Assert.Equal("aapl.us", result.Symbol!.Value);
    }

    // Behavior 8
    [Theory]
    [InlineData("/stock=")]
    [InlineData("/stock")]
    public void Parse_StockPrefixWithoutValidSymbol_ReturnsUnknownCommand(string input)
    {
        ChatCommandParseResult result = ChatCommandParser.Parse(input);
        Assert.Equal(ChatInputKind.UnknownCommand, result.Kind);
        Assert.Null(result.Symbol);
    }

    // Behavior 9
    [Theory]
    [InlineData("/stock=aapl us")]
    [InlineData("/stock=abcdefghijklmnopqrstu")] // 21-char symbol
    public void Parse_StockCommandWithInvalidSymbol_ReturnsUnknownCommand(string input)
    {
        ChatCommandParseResult result = ChatCommandParser.Parse(input);
        Assert.Equal(ChatInputKind.UnknownCommand, result.Kind);
        Assert.Null(result.Symbol);
    }

    // Behavior 10
    [Fact]
    public void Parse_UnknownSlashCommand_ReturnsUnknownCommand()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("/foo");
        Assert.Equal(ChatInputKind.UnknownCommand, result.Kind);
        Assert.Null(result.Symbol);
    }

    // Behavior 11
    [Fact]
    public void Parse_PlainText_ReturnsPlainMessage()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("hello world");
        Assert.Equal(ChatInputKind.PlainMessage, result.Kind);
        Assert.Null(result.Symbol);
    }

    // Behavior 11
    [Fact]
    public void Parse_SlashNotFirstChar_ReturnsPlainMessage()
    {
        ChatCommandParseResult result = ChatCommandParser.Parse("see /stock=aapl.us");
        Assert.Equal(ChatInputKind.PlainMessage, result.Kind);
        Assert.Null(result.Symbol);
    }

    // Behavior 12
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrBlank_ReturnsPlainMessage(string? input)
    {
        ChatCommandParseResult result = ChatCommandParser.Parse(input);
        Assert.Equal(ChatInputKind.PlainMessage, result.Kind);
        Assert.Null(result.Symbol);
    }
}
