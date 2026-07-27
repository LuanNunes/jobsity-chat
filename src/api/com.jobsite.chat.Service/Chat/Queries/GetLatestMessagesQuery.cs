using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Shared.Contracts.Repositories;
using MediatR;

namespace com.jobsite.chat.Service.Chat.Queries;

public sealed record GetLatestMessagesQuery(Guid RoomId) : IRequest<IReadOnlyList<ChatMessageDto>>;

public sealed class GetLatestMessagesQueryHandler(IChatMessageRepository messages)
    : IRequestHandler<GetLatestMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    public const int MessageLimit = 50;

    public async Task<IReadOnlyList<ChatMessageDto>> Handle(GetLatestMessagesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatMessage> latest = await messages.GetLatestAsync(request.RoomId, MessageLimit, cancellationToken);
        return latest
            .Select(ChatMessageDto.FromEntity)
            .ToList();
    }
}
