using System.Text.Json;
using com.jobsite.chat.Shared.Contracts;
using RabbitMQ.Client;

namespace com.jobsite.chat.Shared.Messaging;

internal sealed class RabbitMqPublisher(IRabbitMqConnection connection)
{
    public async Task PublishAsync<T>(string queue, T message, CancellationToken ct)
    {
        IConnection cnn = await connection.GetConnectionAsync(ct);
        await using IChannel channel = await cnn.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, MessagingJson.Options);
        BasicProperties properties = new()
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }
}
