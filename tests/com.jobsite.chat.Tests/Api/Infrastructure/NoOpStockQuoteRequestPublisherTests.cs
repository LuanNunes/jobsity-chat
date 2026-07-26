using com.jobsite.chat.Api.Infrastructure;
using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Shared.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace com.jobsite.chat.Tests.Api.Infrastructure;

// Spec §2.6 / §4.1 (optional): the no-op seam publishes nothing and completes without throwing.
public class NoOpStockQuoteRequestPublisherTests
{
    [Fact]
    public async Task PublishAsync_CompletesWithoutThrow()
    {
        IStockQuoteRequestPublisher publisher =
            new NoOpStockQuoteRequestPublisher(NullLogger<NoOpStockQuoteRequestPublisher>.Instance);
        StockQuoteRequestDto request = new StockQuoteRequestDto(Guid.NewGuid(), "aapl.us");

        await publisher.PublishAsync(request, CancellationToken.None);
    }
}
