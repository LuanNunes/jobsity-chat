using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Shared.Contracts.Repositories;

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

    // Rooms considered already stored (seeded by tests). AddAsync also records here so
    // the handler's duplicate check can observe the just-added rooms in a single flow.
    private List<ChatRoom> Existing { get; } = [];
    public string? LastExistsByNameArg { get; private set; }

    // Seeds a pre-existing room without going through AddAsync (does not affect AddAsyncCallCount).
    public void SeedExisting(ChatRoom room) => Existing.Add(room);

    public Task<bool> ExistsAsync(Guid roomId, CancellationToken ct)
    {
        LastExistsRoomId = roomId;
        return Task.FromResult(ExistsResult);
    }

    // TODO(implementer): this satisfies the IChatRoomRepository.ExistsByNameAsync member the
    // production interface will declare. Case-insensitive, trimmed match against seeded/added rooms.
    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
    {
        LastExistsByNameArg = name;
        string needle = (name).Trim();
        bool exists = Existing
            .Concat(Added)
            .Any(r => string.Equals(r.Name, needle, StringComparison.OrdinalIgnoreCase));
        
        return Task.FromResult(exists);
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
