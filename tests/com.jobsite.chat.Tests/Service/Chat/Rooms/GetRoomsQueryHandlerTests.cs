using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Service.Chat.Rooms;
using com.jobsite.chat.Tests.Service.Fakes;

namespace com.jobsite.chat.Tests.Service.Chat.Rooms;

// Behavior 34: GetRoomsQueryHandler.
public class GetRoomsQueryHandlerTests
{
    private readonly FakeChatRoomRepository _rooms = new();

    private GetRoomsQueryHandler CreateHandler()
        => new(_rooms);

    // Behavior 34
    [Fact]
    public async Task Handle_MapsAllRoomsFromGetAllToDtos()
    {
        DateTimeOffset createdAt = new DateTimeOffset(2026, 7, 25, 9, 0, 0, TimeSpan.Zero);
        ChatRoom general = ChatRoom.Create("General", createdAt);
        ChatRoom random = ChatRoom.Create("Random", createdAt);
        _rooms.GetAllResult = new List<ChatRoom> { general, random };
        GetRoomsQueryHandler handler = CreateHandler();

        IReadOnlyList<ChatRoomDto> result = await handler.Handle(new GetRoomsQuery(), CancellationToken.None);

        Assert.Equal(1, _rooms.GetAllCallCount);
        Assert.Equal(2, result.Count);
        Assert.Equal(general.Id, result[0].Id);
        Assert.Equal("General", result[0].Name);
        Assert.Equal(random.Id, result[1].Id);
        Assert.Equal("Random", result[1].Name);
    }
}
