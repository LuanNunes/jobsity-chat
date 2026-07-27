using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Shared.Contracts;

namespace com.jobsite.chat.Tests.Service.Fakes;

public sealed class FakeStockQuoteRequestPublisher : IStockQuoteRequestPublisher
{
    public List<StockQuoteRequestDto> Published { get; } = new();
    public int PublishCallCount { get; private set; }

    public Task PublishAsync(StockQuoteRequestDto request, CancellationToken ct)
    {
        PublishCallCount++;
        Published.Add(request);
        return Task.CompletedTask;
    }
}
