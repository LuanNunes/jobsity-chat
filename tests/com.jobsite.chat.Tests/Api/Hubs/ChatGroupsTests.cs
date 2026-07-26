using com.jobsite.chat.Api.Hubs;

namespace com.jobsite.chat.Tests.Api.Hubs;

// Spec §5.1 / §7: ChatGroups.Room(id) == "room:{id}" and matches the hub's group-name format.
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
