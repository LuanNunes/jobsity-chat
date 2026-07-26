using System.ComponentModel.DataAnnotations;

namespace com.jobsite.chat.Shared.Messaging;

// Every value is required from the "RabbitMq" configuration section. Properties are init-only
// (immutable, but writable by the configuration binder); a missing or blank value fails
// validation at startup via ValidateDataAnnotations + ValidateOnStart in AddRabbitMqCore.
public sealed record RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required] public string Host { get; init; } = string.Empty;
    [Range(1, 65535)] public int Port { get; init; }
    [Required] public string UserName { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
    [Required] public string VirtualHost { get; init; } = string.Empty;
    [Required] public string RequestsQueue { get; init; } = string.Empty;
    [Required] public string RepliesQueue { get; init; } = string.Empty;
}
