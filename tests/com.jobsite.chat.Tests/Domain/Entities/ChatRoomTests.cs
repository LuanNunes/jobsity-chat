using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Exceptions;

namespace com.jobsite.chat.Tests.Domain.Entities;

// Behavior 18: ChatRoom.Create.
public class ChatRoomTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidName_TrimsNameAndSetsFields()
    {
        ChatRoom room = ChatRoom.Create("  General  ", CreatedAt);
        Assert.Equal("General", room.Name);
        Assert.Equal(CreatedAt, room.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, room.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhitespaceName_ThrowsDomainException(string? name)
    {
        Assert.Throws<DomainException>(() => ChatRoom.Create(name!, CreatedAt));
    }

    [Fact]
    public void Create_NameLongerThan100_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ChatRoom.Create(new string('a', 101), CreatedAt));
    }

    [Fact]
    public void Create_NameExactly100_Succeeds()
    {
        string name = new string('a', 100);
        ChatRoom room = ChatRoom.Create(name, CreatedAt);
        Assert.Equal(name, room.Name);
    }
}
