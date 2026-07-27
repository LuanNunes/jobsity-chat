using com.jobsite.chat.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace com.jobsite.chat.Shared.Messaging;

internal sealed class RabbitMqStockQuoteReplyPublisher(
    RabbitMqPublisher publisher,
    IOptions<RabbitMqOptions> options)
    : IStockQuoteReplyPublisher
{
    public Task PublishAsync(StockQuoteReply reply, CancellationToken ct) =>
        publisher.PublishAsync(options.Value.RepliesQueue, reply, ct);
}
