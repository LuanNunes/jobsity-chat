using System.ComponentModel.DataAnnotations;

namespace com.jobsite.chat.Api.Infrastructure;

// Per-IP fixed-window rate-limit budgets (permits per one-minute window). Bound from the
// "RateLimiting" config section and read at request time so test/host config overrides apply.
public record RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    // Global budget for every non-exempt request from a single client IP.
    [Range(1, int.MaxValue)]
    public int PermitPerMinute { get; init; } = 100;

    // Tighter budget for credential endpoints (login + register) from a single client IP.
    [Range(1, int.MaxValue)]
    public int AuthPermitPerMinute { get; init; } = 10;
}
