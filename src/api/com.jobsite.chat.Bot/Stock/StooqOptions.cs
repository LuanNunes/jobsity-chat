using System.ComponentModel.DataAnnotations;

namespace com.jobsite.chat.Bot.Stock;

public sealed record StooqOptions
{
    public const string SectionName = "Stooq";

    [Required] public string BaseUrl { get; init; } = string.Empty;
}
