using com.jobsite.chat.Domain.Dtos;

namespace com.jobsite.chat.Shared.Contracts;

// RabbitMQ adapter lands later in Shared.
public interface IStockQuoteRequestPublisher
{
    Task PublishAsync(StockQuoteRequestDto request, CancellationToken ct);
}
