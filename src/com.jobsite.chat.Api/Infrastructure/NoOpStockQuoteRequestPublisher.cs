using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace com.jobsite.chat.Api.Infrastructure;

// SEAM: placeholder for the real RabbitMQ publisher (lands in Shared later; one-line DI swap).
internal sealed class NoOpStockQuoteRequestPublisher : IStockQuoteRequestPublisher
{
    private readonly ILogger<NoOpStockQuoteRequestPublisher> _logger;

    public NoOpStockQuoteRequestPublisher(ILogger<NoOpStockQuoteRequestPublisher> logger) => _logger = logger;

    public Task PublishAsync(StockQuoteRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation(
            "STOCK SEAM (no-op): would publish quote request for {Code} in room {RoomId}.",
            request.StockCode, request.RoomId);
        return Task.CompletedTask;
    }
}
