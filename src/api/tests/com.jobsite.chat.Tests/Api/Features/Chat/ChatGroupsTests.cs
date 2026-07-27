using com.jobsite.chat.Api.Features.Chat;

namespace com.jobsite.chat.Tests.Api.Features.Chat;

public class ChatGroupsTests
{
    [Fact]
    public void Room_Guid_ReturnsRoomColonGuidFormat()
    {
        Guid roomId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        string group = ChatGroups.Room(roomId);

        Assert.Equal($"room:{roomId}", group);
        Assert.Equal("room:11111111-2222-3333-4444-555555555555", group);
    }
}
