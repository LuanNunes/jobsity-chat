using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace com.jobsite.chat.Tests.Repository;

// Spec §4 behavior 21: model/schema assertions on ChatDbContext (exercises the config directly,
// not repository logic — expected to PASS from the compile-enablement skeletons).
public sealed class ChatDbContextTests
{
    // Behavior 21: ChatDbContext model declares a non-unique index on (RoomId, SentAtUtc).
    [Fact]
    public void Model_ChatMessage_DeclaresNonUniqueIndexOnRoomIdAndSentAtUtc()
    {
        using SqliteInMemoryFixture fixture = new();
        using ChatDbContext context = fixture.NewChatContext();

        IEntityType entityType = context.Model.FindEntityType(typeof(ChatMessage))!;
        IReadOnlyList<IIndex> indexes = entityType.GetIndexes().ToList();

        IIndex? index = indexes.SingleOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties[0].Name == nameof(ChatMessage.RoomId) &&
            i.Properties[1].Name == nameof(ChatMessage.SentAtUtc));

        Assert.NotNull(index);
        Assert.False(index!.IsUnique);
    }
}
