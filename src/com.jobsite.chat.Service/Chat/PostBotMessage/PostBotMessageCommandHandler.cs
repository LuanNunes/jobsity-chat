using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.ValueObjects;
using com.jobsite.chat.Shared.Abstractions;
using com.jobsite.chat.Service.Exceptions;
using MediatR;

namespace com.jobsite.chat.Service.Chat.PostBotMessage;

// Bot replies re-enter as regular persisted messages (req. 4). Content is never
// command-parsed; the entity trims it. Field validation via the domain factories.
public sealed class PostBotMessageCommandHandler(
    IChatRoomRepository rooms,
    IChatMessageRepository messages,
    TimeProvider clock)
    : IRequestHandler<PostBotMessageCommand, ChatMessageDto>
{
    public async Task<ChatMessageDto> Handle(PostBotMessageCommand request, CancellationToken cancellationToken)
    {
        if (!await rooms.ExistsAsync(request.RoomId, cancellationToken))
        {
            throw new RoomNotFoundException(request.RoomId);
        }

        MessageAuthor author = MessageAuthor.Bot(request.BotDisplayName);
        NewChatMessageDto newChatMessageDto =
            NewChatMessageDto.Create(request.RoomId, author, request.Content, clock);
        ChatMessage message = ChatMessage.Create(newChatMessageDto);
        await messages.AddAsync(message, cancellationToken);
        return ChatMessageDto.FromEntity(message);
    }
}
