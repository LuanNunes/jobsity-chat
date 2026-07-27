using RabbitMQ.Client;

namespace com.jobsite.chat.Shared.Contracts;

public interface IRabbitMqConnection : IAsyncDisposable
{
    Task<IConnection> GetConnectionAsync(CancellationToken ct);
}
