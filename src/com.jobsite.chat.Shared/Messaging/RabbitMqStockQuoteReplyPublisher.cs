using com.jobsite.chat.Shared.Abstractions;
using Microsoft.Extensions.Options;

namespace com.jobsite.chat.Shared.Messaging;

// Publishes a formatted stock-quote reply to the replies queue.
internal sealed class RabbitMqStockQuoteReplyPublisher(
    RabbitMqPublisher publisher,
    IOptions<RabbitMqOptions> options)
    : IStockQuoteReplyPublisher
{
    public Task PublishAsync(StockQuoteReply reply, CancellationToken ct) =>
        publisher.PublishAsync(options.Value.RepliesQueue, reply, ct);
}
