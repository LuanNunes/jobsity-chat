using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Shared.Abstractions;

namespace com.jobsite.chat.Tests.Service.Fakes;

// Hand-rolled fake recording calls; GetLatestAsync result and captured args are inspectable.
public sealed class FakeChatMessageRepository : IChatMessageRepository
{
    public List<ChatMessage> Added { get; } = new();
    public int AddAsyncCallCount { get; private set; }

    public IReadOnlyList<ChatMessage> GetLatestResult { get; set; } = new List<ChatMessage>();
    public Guid? LastGetLatestRoomId { get; private set; }
    public int? LastGetLatestCount { get; private set; }
    public int GetLatestCallCount { get; private set; }

    public Task AddAsync(ChatMessage message, CancellationToken ct)
    {
        AddAsyncCallCount++;
        Added.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatMessage>> GetLatestAsync(Guid roomId, int count, CancellationToken ct)
    {
        GetLatestCallCount++;
        LastGetLatestRoomId = roomId;
        LastGetLatestCount = count;
        return Task.FromResult(GetLatestResult);
    }
}
