using com.jobsite.chat.Shared.Messaging;

namespace com.jobsite.chat.Shared.Abstractions;

// Publishes bot stock-quote replies (Bot -> RabbitMQ RepliesQueue -> Api consumer).
public interface IStockQuoteReplyPublisher
{
    Task PublishAsync(StockQuoteReply reply, CancellationToken ct);
}
