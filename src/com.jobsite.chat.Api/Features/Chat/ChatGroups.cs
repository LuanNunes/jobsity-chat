namespace com.jobsite.chat.Api.Features.Chat;

// SignalR group-name centralization; hub + reply consumer share it.
public static class ChatGroups
{
    public static string Room(Guid roomId) => $"room:{roomId}";
}
