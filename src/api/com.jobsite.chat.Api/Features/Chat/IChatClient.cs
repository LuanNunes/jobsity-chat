using com.jobsite.chat.Domain.Dtos;

namespace com.jobsite.chat.Api.Features.Chat;

// Strongly-typed SignalR client contract (Api presentation type, not a cross-layer port).
public interface IChatClient
{
    Task ReceiveMessage(ChatMessageDto message);              // broadcast persisted msg to room group
    Task RoomCreated(ChatRoomDto room);                       // broadcast new room to all clients
    Task LoadHistory(IReadOnlyList<ChatMessageDto> messages); // latest-50 to joining caller
    Task CommandAccepted(string stockCode);                   // ephemeral ack (StockCommandQueued)
    Task CommandRejected(string reason);                      // ephemeral (UnknownCommandRejected)
    Task ErrorOccurred(string reason);                        // ephemeral (domain/room/unexpected)
}
