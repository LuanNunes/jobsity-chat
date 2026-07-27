using com.jobsite.chat.Domain.Dtos;

namespace com.jobsite.chat.Shared.Contracts;

public interface IStockQuoteRequestPublisher
{
    Task PublishAsync(StockQuoteRequestDto request, CancellationToken ct);
}
