using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Domain.Rules;
using com.jobsite.chat.Domain.ValueObjects;
using com.jobsite.chat.Shared.Abstractions;
using com.jobsite.chat.Service.Exceptions;
using MediatR;

namespace com.jobsite.chat.Service.Chat.SendMessage;

// Room check -> parse -> branch. No field validation here: the domain factories throw.
public sealed class SendMessageCommandHandler(
    IChatRoomRepository rooms,
    IChatMessageRepository messages,
    IStockQuoteRequestPublisher stockPublisher,
    TimeProvider clock)
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    public async Task<SendMessageResult> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (!await rooms.ExistsAsync(request.RoomId, cancellationToken))
        {
            throw new RoomNotFoundException(request.RoomId);
        }

        ChatCommandParseResult parsed = ChatCommandParser.Parse(request.Content);
        switch (parsed.Kind)
        {
            case ChatInputKind.StockCommand:
                string stockCode = parsed.Symbol!.Value;
                await stockPublisher.PublishAsync(
                    new StockQuoteRequestDto(request.RoomId, stockCode), cancellationToken);
                return new SendMessageResult(SendMessageOutcome.StockCommandQueued, StockCode: stockCode);

            case ChatInputKind.UnknownCommand:
                return new SendMessageResult(SendMessageOutcome.UnknownCommandRejected);

            case ChatInputKind.PlainMessage:
            default:
                MessageAuthor author = MessageAuthor.User(request.AuthorUserId, request.AuthorDisplayName);
                ChatMessage message = ChatMessage.Create(request.RoomId, author, request.Content, clock.GetUtcNow());
                await messages.AddAsync(message, cancellationToken);
                ChatMessageDto dto = new ChatMessageDto(
                    message.Id,
                    message.RoomId,
                    message.Author.DisplayName,
                    message.Content,
                    message.Author.IsBot,
                    message.SentAtUtc);
                return new SendMessageResult(SendMessageOutcome.MessagePersisted, Message: dto);
        }
    }
}
