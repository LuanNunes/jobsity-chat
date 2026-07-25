using com.jobsite.chat.Domain.Validation;

namespace com.jobsite.chat.Domain.ValueObjects;

// Flexible authorship value object (req. 4): a message is authored either by an
// Identity user or by the bot (null UserId).
public sealed record MessageAuthor
{
    public const string DefaultBotName = "StockBot";
    public const int MaxDisplayNameLength = 100;

    private MessageAuthor(string? userId, string displayName)
    {
        UserId = userId;
        DisplayName = displayName;
    }

    public string? UserId { get; }        // ASP.NET Identity user id; null => bot
    public string DisplayName { get; }    // trimmed
    public bool IsBot => UserId is null;

    public static MessageAuthor User(string userId, string displayName)
    {
        string id = Ensure.NotNullOrWhiteSpace(userId, nameof(userId));
        string name = Ensure.MaxLength(
            Ensure.NotNullOrWhiteSpace(displayName, nameof(displayName)),
            MaxDisplayNameLength,
            nameof(displayName));
        return new MessageAuthor(id, name);
    }

    public static MessageAuthor Bot(string displayName = DefaultBotName)
    {
        string name = Ensure.MaxLength(
            Ensure.NotNullOrWhiteSpace(displayName, nameof(displayName)),
            MaxDisplayNameLength,
            nameof(displayName));
        return new MessageAuthor(null, name);
    }
}
