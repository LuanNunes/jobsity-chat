using RabbitMQ.Client;

namespace com.jobsite.chat.Shared.Contracts;

// Owns the single, lazily-opened, auto-recovering RabbitMQ connection for a process.
// Callers create one IChannel per consumer (IChannel is not thread-safe) and a short-lived channel per publish.
public interface IRabbitMqConnection : IAsyncDisposable
{
    Task<IConnection> GetConnectionAsync(CancellationToken ct);
}
