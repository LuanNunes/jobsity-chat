namespace com.jobsite.chat.Api.Features.Chat;

public static class ChatGroups
{
    public static string Room(Guid roomId) => $"room:{roomId}";
}
