using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Enums;
using MediatR;

namespace com.jobsite.chat.Service.Chat.SendMessage;

public sealed record SendMessageCommand(
    Guid RoomId, string AuthorUserId, string AuthorDisplayName, string Content)
    : IRequest<SendMessageResult>;

public sealed record SendMessageResult(
    SendMessageOutcome Outcome,
    ChatMessageDto? Message = null,    // set iff MessagePersisted
    string? StockCode = null);         // set iff StockCommandQueued
