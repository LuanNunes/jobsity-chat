using com.jobsite.chat.Bot.Consumers;
using com.jobsite.chat.Bot.Stock;
using com.jobsite.chat.Shared.Abstractions;
using com.jobsite.chat.Shared.Messaging;
using Microsoft.Extensions.Options;

public class Program
{
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddRabbitMqCore(builder.Configuration);
        builder.Services.AddStockReplyPublisher();

        builder.Services.AddOptions<StooqOptions>()
            .Bind(builder.Configuration.GetSection(StooqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddHttpClient<IStockPriceClient, StooqStockPriceClient>((serviceProvider, client) =>
            {
                StooqOptions stooq = serviceProvider.GetRequiredService<IOptions<StooqOptions>>().Value;
                client.BaseAddress = new Uri(stooq.BaseUrl);
            })
            .AddStandardResilienceHandler();

        builder.Services.AddHostedService<StockRequestConsumer>();

        IHost host = builder.Build();
        host.Run();
    }
}
