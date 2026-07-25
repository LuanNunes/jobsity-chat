using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Service.Abstractions;

namespace com.jobsite.chat.Tests.Service.Fakes;

// Hand-rolled fake recording calls; ExistsAsync result is configurable.
public sealed class FakeChatRoomRepository : IChatRoomRepository
{
    public bool ExistsResult { get; set; } = true;
    public Guid? LastExistsRoomId { get; private set; }

    public List<ChatRoom> Added { get; } = new();
    public int AddAsyncCallCount { get; private set; }

    public IReadOnlyList<ChatRoom> GetAllResult { get; set; } = new List<ChatRoom>();
    public int GetAllCallCount { get; private set; }

    public Task<bool> ExistsAsync(Guid roomId, CancellationToken ct)
    {
        LastExistsRoomId = roomId;
        return Task.FromResult(ExistsResult);
    }

    public Task<IReadOnlyList<ChatRoom>> GetAllAsync(CancellationToken ct)
    {
        GetAllCallCount++;
        return Task.FromResult(GetAllResult);
    }

    public Task AddAsync(ChatRoom room, CancellationToken ct)
    {
        AddAsyncCallCount++;
        Added.Add(room);
        return Task.CompletedTask;
    }
}
