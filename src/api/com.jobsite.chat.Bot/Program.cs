using com.jobsite.chat.Bot.Consumers;
using com.jobsite.chat.Bot.Stock;
using com.jobsite.chat.Shared.Contracts;
using com.jobsite.chat.Shared.Logging;
using com.jobsite.chat.Shared.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            WebApplication app = BuildApplication(args);
            app.MapHealthChecks("/health");
            await app.RunAsync();
            return 0;
        }
        catch (Exception exception) when (!IsHostStopSignal(exception))
        {
            Log.Fatal(exception, "Bot host terminated unexpectedly.");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static bool IsHostStopSignal(Exception exception) =>
        exception is HostAbortedException || exception.GetType().Name == "StopTheHostException";

    private static WebApplication BuildApplication(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSerilog(
            (services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .ConfigureConsoleSink(builder.Environment.IsDevelopment()),
            preserveStaticLogger: true);

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

        builder.Services.AddHealthChecks()
            .AddRabbitMqHealthCheck();

        return builder.Build();
    }
}
