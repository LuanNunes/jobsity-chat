using System.Text.Json;
using com.jobsite.chat.Bot.Stock;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.ValueObjects;
using com.jobsite.chat.Shared.Abstractions;
using com.jobsite.chat.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace com.jobsite.chat.Bot.Consumers;

// Consumes stock-quote requests, fetches + parses the stooq CSV, formats a reply, and publishes it.
// A failed deserialize throws (base nacks, no requeue); HTTP failures still produce a room-visible reply.
internal sealed class StockRequestConsumer(
    IRabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    IStockPriceClient priceClient,
    IStockQuoteReplyPublisher replyPublisher,
    ILogger<StockRequestConsumer> logger)
    : RabbitMqQueueConsumer(connection, options.Value.RequestsQueue, logger)
{
    protected override async Task ProcessAsync(ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        StockQuoteRequestDto request =
            JsonSerializer.Deserialize<StockQuoteRequestDto>(body.Span, MessagingJson.Options)
            ?? throw new JsonException("Stock request payload deserialized to null.");
        
        await HandleRequestAsync(request, ct);
    }

    // Fetch -> parse -> format -> publish reply. HTTP failures after retries still produce a room-visible reply.
    private async Task HandleRequestAsync(StockQuoteRequestDto request, CancellationToken ct)
    {
        StockQuoteReply reply = await BuildReplyAsync(request, ct);
        await replyPublisher.PublishAsync(reply, ct);
    }

    private async Task<StockQuoteReply> BuildReplyAsync(StockQuoteRequestDto request, CancellationToken ct)
    {
        try
        {
            string csv = await priceClient.GetQuoteCsvAsync(request.StockCode, ct);
            StockQuote? quote = StooqCsvParser.Parse(csv);
            string text = quote is not null
                ? quote.ToQuoteMessage()
                : StockQuote.NotFoundMessage(request.StockCode);
            
            return new StockQuoteReply(request.RoomId, text, quote is not null);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Stooq lookup for {Code} failed after retries.", request.StockCode);
            return new StockQuoteReply(request.RoomId, StockQuote.NotFoundMessage(request.StockCode), false);
        }
    }
}
