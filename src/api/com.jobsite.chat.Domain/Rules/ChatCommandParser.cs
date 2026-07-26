using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Domain.Rules;

public sealed record ChatCommandParseResult(ChatInputKindEnum KindEnum, StockSymbol? Symbol)
{
    public static ChatCommandParseResult Plain()
        => new(ChatInputKindEnum.PlainMessage, null);

    public static ChatCommandParseResult Stock(StockSymbol symbol)
        => new(ChatInputKindEnum.StockCommand, symbol);

    public static ChatCommandParseResult Unknown()
        => new(ChatInputKindEnum.UnknownCommand, null);
}

// Pure, static, deterministic: "what is a command" is a domain rule.
public static class ChatCommandParser
{
    private const string StockPrefix = "/stock=";

    public static ChatCommandParseResult Parse(string? content)
    {
        string? trimmed = content?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed[0] != '/')
        {
            return ChatCommandParseResult.Plain();
        }

        if (!trimmed.StartsWith(StockPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ChatCommandParseResult.Unknown();
        }
            
        string raw = trimmed[StockPrefix.Length..];
        return StockSymbol.TryCreate(raw, out StockSymbol? symbol) 
            ? ChatCommandParseResult.Stock(symbol!)
            : ChatCommandParseResult.Unknown();
    }
}
