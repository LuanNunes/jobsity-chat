using FluentValidation;
using com.jobsite.chat.Domain.Validation;

namespace com.jobsite.chat.Domain.ValueObjects;

// Flexible authorship value object (req. 4): a message is authored either by an
// Identity user or by the bot (null UserId).
public sealed record MessageAuthor
{
    public const string DefaultBotName = "StockBot";
    private const int MaxDisplayNameLength = 100;

    private MessageAuthor(string? userId, string displayName)
    {
        UserId = userId;
        DisplayName = displayName;
    }

    public string? UserId { get; }        // ASP.NET Identity user id; null => bot
    public string DisplayName { get; }    // trimmed
    public bool IsBot => UserId is null;

    private static readonly InlineValidator<MessageAuthor> Rules = new()
    {
        v => v.RuleFor(a => a.DisplayName).NotEmpty().MaximumLength(MaxDisplayNameLength),
        v => v.RuleFor(a => a.UserId).NotEmpty().When(a => a.UserId is not null),
    };

    public static MessageAuthor User(string userId, string displayName)
    {
        MessageAuthor author = new(userId?.Trim() ?? string.Empty, displayName?.Trim() ?? string.Empty);
        Rules.ValidateAndThrowDomain(author);
        return author;
    }

    public static MessageAuthor Bot(string displayName = DefaultBotName)
    {
        MessageAuthor author = new(null, displayName?.Trim() ?? string.Empty);
        Rules.ValidateAndThrowDomain(author);
        return author;
    }
}
