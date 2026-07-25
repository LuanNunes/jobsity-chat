using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Domain.ValueObjects;

namespace com.jobsite.chat.Domain.Rules;

public sealed record ChatCommandParseResult(ChatInputKind Kind, StockSymbol? Symbol)
{
    public static ChatCommandParseResult Plain()
        => new(ChatInputKind.PlainMessage, null);

    public static ChatCommandParseResult Stock(StockSymbol symbol)
        => new(ChatInputKind.StockCommand, symbol);

    public static ChatCommandParseResult Unknown()
        => new(ChatInputKind.UnknownCommand, null);
}

// Pure, static, deterministic: "what is a command" is a domain rule.
public static class ChatCommandParser
{
    private const string StockPrefix = "/stock=";

    public static ChatCommandParseResult Parse(string? content)
    {
        string? trimmed = content?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return ChatCommandParseResult.Plain();
        }

        if (trimmed[0] != '/')
        {
            return ChatCommandParseResult.Plain();
        }

        if (trimmed.StartsWith(StockPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string raw = trimmed[StockPrefix.Length..];
            if (StockSymbol.TryCreate(raw, out StockSymbol? symbol))
            {
                return ChatCommandParseResult.Stock(symbol!);
            }
        }

        return ChatCommandParseResult.Unknown();
    }
}
