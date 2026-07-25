using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Shared.Abstractions;
using MediatR;

namespace com.jobsite.chat.Service.Chat.GetLatestMessages;

public sealed record GetLatestMessagesQuery(Guid RoomId) : IRequest<IReadOnlyList<ChatMessageDto>>;

// Calls messages.GetLatestAsync(RoomId, MessageLimit, ct); maps to DTOs preserving order.
// Unknown room (including Guid.Empty) => empty list (no throw on the read path).
public sealed class GetLatestMessagesQueryHandler(IChatMessageRepository messages)
    : IRequestHandler<GetLatestMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    public const int MessageLimit = 50;   // handler owns the limit; not caller-supplied

    public async Task<IReadOnlyList<ChatMessageDto>> Handle(GetLatestMessagesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatMessage> latest = await messages.GetLatestAsync(request.RoomId, MessageLimit, cancellationToken);
        return latest
            .Select(message => new ChatMessageDto(
                message.Id,
                message.RoomId,
                message.Author.DisplayName,
                message.Content,
                message.Author.IsBot,
                message.SentAtUtc))
            .ToList();
    }
}
