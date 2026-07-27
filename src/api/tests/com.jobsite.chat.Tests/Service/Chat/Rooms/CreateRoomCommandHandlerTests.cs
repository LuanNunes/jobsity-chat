using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Service.Chat.Commands;
using com.jobsite.chat.Tests.Service.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace com.jobsite.chat.Tests.Service.Chat.Rooms;

public class CreateRoomCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 25, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeChatRoomRepository _rooms = new();
    private readonly FakeTimeProvider _clock = new(FixedNow);

    private CreateRoomCommandHandler CreateHandler()
        => new(_rooms, _clock);

    [Fact]
    public async Task Handle_ValidName_AddsRoomAndReturnsDto()
    {
        CreateRoomCommandHandler handler = CreateHandler();

        ChatRoomDto dto = await handler.Handle(new CreateRoomCommand("General"), CancellationToken.None);

        ChatRoom added = Assert.Single(_rooms.Added);
        Assert.Equal("General", added.Name);
        Assert.Equal(added.Id, dto.Id);
        Assert.Equal("General", dto.Name);
    }

    [Fact]
    public async Task Handle_EmptyName_ThrowsDomainExceptionAndDoesNotAdd()
    {
        CreateRoomCommandHandler handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateRoomCommand(""), CancellationToken.None));
        Assert.Equal(0, _rooms.AddAsyncCallCount);
    }

    [Fact]
    public async Task Handle_NameLongerThan100_ThrowsDomainExceptionAndDoesNotAdd()
    {
        CreateRoomCommandHandler handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateRoomCommand(new string('a', 101)), CancellationToken.None));
        Assert.Equal(0, _rooms.AddAsyncCallCount);
    }

    [Fact]
    public async Task Handle_DuplicateNameDifferentCase_ThrowsDomainExceptionAndDoesNotAdd()
    {
        _rooms.SeedExisting(ChatRoom.Create("General", FixedNow));
        CreateRoomCommandHandler handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateRoomCommand("general"), CancellationToken.None));

        Assert.Equal(0, _rooms.AddAsyncCallCount);
        Assert.Empty(_rooms.Added);
    }

    [Fact]
    public async Task Handle_DuplicateNameWithSurroundingWhitespace_ThrowsDomainExceptionAndDoesNotAdd()
    {
        _rooms.SeedExisting(ChatRoom.Create("General", FixedNow));
        CreateRoomCommandHandler handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateRoomCommand("  General  "), CancellationToken.None));

        Assert.Equal(0, _rooms.AddAsyncCallCount);
        Assert.Empty(_rooms.Added);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsWithNameInMessage()
    {
        _rooms.SeedExisting(ChatRoom.Create("General", FixedNow));
        CreateRoomCommandHandler handler = CreateHandler();

        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateRoomCommand("general"), CancellationToken.None));

        Assert.Contains("general", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_UniqueNameWithExistingRooms_AddsRoom()
    {
        _rooms.SeedExisting(ChatRoom.Create("General", FixedNow));
        CreateRoomCommandHandler handler = CreateHandler();

        ChatRoomDto dto = await handler.Handle(new CreateRoomCommand("Random"), CancellationToken.None);

        ChatRoom added = Assert.Single(_rooms.Added);
        Assert.Equal("Random", added.Name);
        Assert.Equal("Random", dto.Name);
    }
}
