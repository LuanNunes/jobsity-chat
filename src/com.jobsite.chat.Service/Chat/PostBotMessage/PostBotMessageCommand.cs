using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.ValueObjects;
using MediatR;

namespace com.jobsite.chat.Service.Chat.PostBotMessage;

public sealed record PostBotMessageCommand(
    Guid RoomId,
    string Content,
    string BotDisplayName = MessageAuthor.DefaultBotName)
    : IRequest<ChatMessageDto>;
