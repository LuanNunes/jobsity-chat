using com.jobsite.chat.Domain.Dtos;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Domain.ValueObjects;
using com.jobsite.chat.Service.Chat.GetLatestMessages;
using com.jobsite.chat.Tests.Service.Fakes;

namespace com.jobsite.chat.Tests.Service.Chat.GetLatestMessages;

// Behaviors 29–31: GetLatestMessagesQueryHandler.
public class GetLatestMessagesQueryHandlerTests
{
    private readonly FakeChatMessageRepository _messages = new();

    private GetLatestMessagesQueryHandler CreateHandler()
        => new(_messages);

    // Behavior 29
    [Fact]
    public async Task Handle_CallsGetLatestWithCountFifty()
    {
        Guid roomId = Guid.NewGuid();
        GetLatestMessagesQueryHandler handler = CreateHandler();

        await handler.Handle(new GetLatestMessagesQuery(roomId), CancellationToken.None);

        Assert.Equal(1, _messages.GetLatestCallCount);
        Assert.Equal(roomId, _messages.LastGetLatestRoomId);
        Assert.Equal(50, _messages.LastGetLatestCount);
        Assert.Equal(50, GetLatestMessagesQueryHandler.MessageLimit);
    }

    // Behavior 30
    [Fact]
    public async Task Handle_MapsEntitiesToDtosPreservingOrderAndFields()
    {
        Guid roomId = Guid.NewGuid();
        DateTimeOffset sentAt = new DateTimeOffset(2026, 7, 25, 9, 0, 0, TimeSpan.Zero);
        ChatMessage first = ChatMessage.Create(new NewChatMessageDto(roomId, MessageAuthor.User("u1", "Ana"), "first", sentAt));
        ChatMessage second = ChatMessage.Create(new NewChatMessageDto(roomId, MessageAuthor.Bot(), "second", sentAt.AddMinutes(1)));
        _messages.GetLatestResult = new List<ChatMessage> { first, second };
        GetLatestMessagesQueryHandler handler = CreateHandler();

        IReadOnlyList<ChatMessageDto> result = await handler.Handle(new GetLatestMessagesQuery(roomId), CancellationToken.None);

        Assert.Equal(2, result.Count);

        Assert.Equal(first.Id, result[0].Id);
        Assert.Equal(roomId, result[0].RoomId);
        Assert.Equal("Ana", result[0].AuthorDisplayName);
        Assert.False(result[0].IsFromBot);
        Assert.Equal("first", result[0].Content);
        Assert.Equal(sentAt, result[0].SentAtUtc);

        Assert.Equal(second.Id, result[1].Id);
        Assert.Equal("StockBot", result[1].AuthorDisplayName);
        Assert.True(result[1].IsFromBot);
        Assert.Equal("second", result[1].Content);
    }

    // Behavior 31
    [Fact]
    public async Task Handle_EmptyRepositoryResult_ReturnsEmptyListNoThrow()
    {
        _messages.GetLatestResult = new List<ChatMessage>();
        GetLatestMessagesQueryHandler handler = CreateHandler();

        IReadOnlyList<ChatMessageDto> result = await handler.Handle(new GetLatestMessagesQuery(Guid.Empty), CancellationToken.None);

        Assert.Empty(result);
    }
}
