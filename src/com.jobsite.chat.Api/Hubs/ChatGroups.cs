namespace com.jobsite.chat.Api.Hubs;

// SignalR group-name centralization; hub + reply consumer share it.
public static class ChatGroups
{
    public static string Room(Guid roomId) => $"room:{roomId}";
}
