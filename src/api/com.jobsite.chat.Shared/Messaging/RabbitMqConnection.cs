using com.jobsite.chat.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace com.jobsite.chat.Shared.Messaging;

// Singleton. Opens ONE IConnection on first use, guarded by a semaphore (double-checked),
// with a Polly exponential-backoff retry so a broker that is not yet up does not crash startup.
internal sealed class RabbitMqConnection : IRabbitMqConnection
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(5);

    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly ResiliencePipeline _connectPipeline;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        RabbitMqOptions value = options.Value;
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = value.Host,
            Port = value.Port,
            UserName = value.UserName,
            Password = value.Password,
            VirtualHost = value.VirtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
        _connectPipeline = BuildConnectPipeline();
    }

    // Retry the connect (MaxAttempts total = 1 initial + MaxAttempts-1 retries) with exponential
    // backoff + jitter; never retry on cancellation. Mirrors the HttpClient resilience used by the Bot.
    private ResiliencePipeline BuildConnectPipeline() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                MaxRetryAttempts = MaxAttempts - 1,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = BaseDelay,
                MaxDelay = MaxDelay,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "RabbitMQ connection attempt {Attempt}/{Max} failed; retrying in {Delay}.",
                        args.AttemptNumber + 1, MaxAttempts, args.RetryDelay);
                    return default;
                }
            })
            .Build();

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        IConnection? existing = _connection;

        return existing is { IsOpen: true }
            ? existing
            : await OpenAsync(ct);
    }

    private async Task<IConnection> OpenAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            IConnection? existing = _connection;
            IConnection connection = existing is { IsOpen: true }
                ? existing
                : await ConnectWithRetryAsync(ct);
            _connection = connection;
            return connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IConnection> ConnectWithRetryAsync(CancellationToken ct)
    {
        try
        {
            return await _connectPipeline.ExecuteAsync(
                async token => await _factory.CreateConnectionAsync(token), ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "RabbitMQ connection failed after {Attempts} attempts.", MaxAttempts);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        IConnection? connection = _connection;
        _connection = null;
        _gate.Dispose();

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }
}
