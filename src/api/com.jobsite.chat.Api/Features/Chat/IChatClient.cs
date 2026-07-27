using com.jobsite.chat.Domain.Dtos;

namespace com.jobsite.chat.Api.Features.Chat;

public interface IChatClient
{
    Task ReceiveMessage(ChatMessageDto message);
    Task RoomCreated(ChatRoomDto room);
    Task LoadHistory(IReadOnlyList<ChatMessageDto> messages);
    Task CommandAccepted(string stockCode);
    Task CommandRejected(string reason);
    Task ErrorOccurred(string reason);
}
