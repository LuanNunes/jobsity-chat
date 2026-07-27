using com.jobsite.chat.Shared.Messaging;

namespace com.jobsite.chat.Shared.Contracts;

public interface IStockQuoteReplyPublisher
{
    Task PublishAsync(StockQuoteReply reply, CancellationToken ct);
}
