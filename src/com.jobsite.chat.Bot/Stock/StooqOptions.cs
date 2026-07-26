using System.ComponentModel.DataAnnotations;

namespace com.jobsite.chat.Bot.Stock;

// stooq client configuration, required from the "Stooq" section; a missing BaseUrl fails at startup.
public sealed record StooqOptions
{
    public const string SectionName = "Stooq";

    [Required] public string BaseUrl { get; init; } = string.Empty;
}
