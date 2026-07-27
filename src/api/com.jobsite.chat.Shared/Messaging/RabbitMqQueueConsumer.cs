using com.jobsite.chat.Shared.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace com.jobsite.chat.Shared.Messaging;

public abstract class RabbitMqQueueConsumer(IRabbitMqConnection connection, string queueName, ILogger logger)
    : BackgroundService
{
    private const ushort PrefetchCount = 8;
    private static readonly TimeSpan ConnectRetryDelay = TimeSpan.FromSeconds(5);

    private IChannel? _channel;

    protected ILogger Logger { get; } = logger;

    protected abstract Task ProcessAsync(ReadOnlyMemory<byte> body, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IChannel channel = await ConnectUntilReadyAsync(stoppingToken);
        _channel = channel;

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += (_, args) => OnMessageAsync(channel, args, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private async Task<IChannel> ConnectUntilReadyAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            IChannel? channel = await TryOpenChannelAsync(ct);

            if (channel is not null)
            {
                return channel;
            }

            await Task.Delay(ConnectRetryDelay, ct);
        }

        ct.ThrowIfCancellationRequested();
        throw new OperationCanceledException(ct);
    }

    private async Task<IChannel?> TryOpenChannelAsync(CancellationToken ct)
    {
        try
        {
            IConnection brokerConnection = await connection.GetConnectionAsync(ct);
            IChannel channel = await brokerConnection.CreateChannelAsync(cancellationToken: ct);
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, cancellationToken: ct);
            return channel;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(exception, "Could not open a RabbitMQ channel for {Queue}; retrying in {Delay}.", queueName, ConnectRetryDelay);
            return null;
        }
    }

    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken ct)
    {
        try
        {
            await ProcessAsync(args.Body, ct);
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to process a message from {Queue}; discarding (no requeue).", queueName);
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        IChannel? channel = _channel;
        _channel = null;

        if (channel is not null)
        {
            await channel.DisposeAsync();
        }
    }
}
