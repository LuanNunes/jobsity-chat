using com.jobsite.chat.Shared.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace com.jobsite.chat.Shared.Messaging;

public static class MessagingServiceCollectionExtensions
{

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
