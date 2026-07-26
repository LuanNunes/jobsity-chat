using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Enums;
using com.jobsite.chat.Domain.Rules;
using com.jobsite.chat.Domain.ValueObjects;
using com.jobsite.chat.Service.Exceptions;
using com.jobsite.chat.Shared.Contracts;
using com.jobsite.chat.Shared.Contracts.Repositories;
using MediatR;

namespace com.jobsite.chat.Service.Chat.Commands;

public sealed record SendMessageCommand(
    Guid RoomId, string AuthorUserId, string AuthorDisplayName, string Content)
    : IRequest<SendMessageResult>;

public sealed record SendMessageResult(
    SendMessageOutcome Outcome,
    ChatMessageDto? Message = null,    // set iff MessagePersisted
    string? StockCode = null);         // set iff StockCommandQueued

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

        Dictionary<ChatInputKind, Func<Task<SendMessageResult>>> handlers = new()
        {
            [ChatInputKind.StockCommand] = async () =>
            {
                string stockCode = parsed.Symbol!.Value;
                await stockPublisher.PublishAsync(new StockQuoteRequestDto(request.RoomId, stockCode), cancellationToken);

                return new SendMessageResult(SendMessageOutcome.StockCommandQueued, StockCode: stockCode);
            },

            [ChatInputKind.UnknownCommand] = () =>
                Task.FromResult(new SendMessageResult(SendMessageOutcome.UnknownCommandRejected)),
        };

        // Anything not explicitly handled (PlainMessage and any future kind) persists a plain message.
        return handlers.TryGetValue(parsed.Kind, out Func<Task<SendMessageResult>>? handler)
            ? await handler()
            : await PersistPlainMessageAsync(request, cancellationToken);
    }

    private async Task<SendMessageResult> PersistPlainMessageAsync(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        MessageAuthor author = MessageAuthor.User(request.AuthorUserId, request.AuthorDisplayName);
        ChatMessage message = ChatMessage.Create(NewChatMessageDto.Create(request.RoomId, author, request.Content, clock));

        await messages.AddAsync(message, cancellationToken);

        return new SendMessageResult(SendMessageOutcome.MessagePersisted, ChatMessageDto.FromEntity(message));
    }
}
