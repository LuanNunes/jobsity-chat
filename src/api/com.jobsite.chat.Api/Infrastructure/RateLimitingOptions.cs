using System.ComponentModel.DataAnnotations;

namespace com.jobsite.chat.Api.Infrastructure;

public record RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, int.MaxValue)]
    public int PermitPerMinute { get; init; } = 100;

    [Range(1, int.MaxValue)]
    public int AuthPermitPerMinute { get; init; } = 10;
}
