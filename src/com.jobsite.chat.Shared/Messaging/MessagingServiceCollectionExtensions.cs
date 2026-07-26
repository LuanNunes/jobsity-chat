using com.jobsite.chat.Shared.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
