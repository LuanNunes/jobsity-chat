using com.jobsite.chat.Shared.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace com.jobsite.chat.Shared.Messaging;

public static class MessagingServiceCollectionExtensions
{
    // Binds RabbitMqOptions and registers the shared singleton connection. Call once per process (Api + Bot).
    extension(IServiceCollection services)
    {
        public IServiceCollection AddRabbitMqCore(IConfiguration configuration)
        {
            services.AddOptions<RabbitMqOptions>()
                .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
            services.AddSingleton<RabbitMqPublisher>();
            return services;
        }

        public IServiceCollection AddStockRequestPublisher()
        {
            services.AddSingleton<IStockQuoteRequestPublisher, RabbitMqStockQuoteRequestPublisher>();
            return services;
        }

        public IServiceCollection AddStockReplyPublisher()
        {
            services.AddSingleton<IStockQuoteReplyPublisher, RabbitMqStockQuoteReplyPublisher>();
            return services;
        }
    }

    // Registers the RabbitMQ readiness check on an existing health-checks builder. Optional tags let
    // the caller scope it to a probe (e.g. "ready"). Reused by both the Api and the Bot. A short
    // timeout keeps the probe fast: a broker that is down must report Unhealthy quickly rather than
    // sit through the connection's startup-resilience retry pipeline.
    extension(IHealthChecksBuilder builder)
    {
        public IHealthChecksBuilder AddRabbitMqHealthCheck(IEnumerable<string>? tags = null) =>
            builder.AddCheck<RabbitMqHealthCheck>(
                "rabbitmq",
                failureStatus: null,
                tags: tags ?? [],
                timeout: TimeSpan.FromSeconds(5));
    }
}
