using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.Extensions.Options;

namespace com.jobsite.chat.Shared.Messaging;

// Publishes a stock-quote request to the requests queue.
internal sealed class RabbitMqStockQuoteRequestPublisher(
    RabbitMqPublisher publisher,
    IOptions<RabbitMqOptions> options)
    : IStockQuoteRequestPublisher
{
    public Task PublishAsync(StockQuoteRequestDto request, CancellationToken ct) =>
        publisher.PublishAsync(options.Value.RequestsQueue, request, ct);
}
