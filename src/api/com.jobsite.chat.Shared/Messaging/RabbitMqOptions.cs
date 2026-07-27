using System.ComponentModel.DataAnnotations;

namespace com.jobsite.chat.Shared.Messaging;

public sealed record RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Uri { get; init; } = string.Empty;
    [Required] public string Host { get; init; } = string.Empty;
    [Range(1, 65535)] public int Port { get; init; }
    [Required] public string UserName { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
    [Required] public string VirtualHost { get; init; } = string.Empty;
    [Required] public string RequestsQueue { get; init; } = string.Empty;
    [Required] public string RepliesQueue { get; init; } = string.Empty;
}
