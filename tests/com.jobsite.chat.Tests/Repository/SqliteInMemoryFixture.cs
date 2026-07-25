using com.jobsite.chat.Repository.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Tests.Repository;

// EF Core SQLite in-memory harness (spec §4): a single OPEN SqliteConnection kept alive for
// the DB lifetime so multiple DbContexts on that connection share the same in-memory database.
// EnsureCreated no-ops on a non-empty DB, so ChatDbContext and AppIdentityDbContext must NEVER
// share a connection — each factory below opens its OWN connection.
public sealed class SqliteInMemoryFixture : IDisposable
{
    private readonly SqliteConnection _chatConnection;
    private readonly SqliteConnection _identityConnection;

    public SqliteInMemoryFixture()
    {
        _chatConnection = new SqliteConnection("Data Source=:memory:");
        _chatConnection.Open();

        _identityConnection = new SqliteConnection("Data Source=:memory:");
        _identityConnection.Open();

        using ChatDbContext seed = NewChatContext();
        seed.Database.EnsureCreated();

        using AppIdentityDbContext identitySeed = NewIdentityContext();
        identitySeed.Database.EnsureCreated();
    }

    // Raw connection for chat-schema tests that need a direct SqliteCommand (behavior 20).
    public SqliteConnection ChatConnection => _chatConnection;

    // A fresh ChatDbContext bound to the shared chat connection. Distinct instances prove
    // persistence across contexts (behaviors 1, 9).
    public ChatDbContext NewChatContext()
    {
        DbContextOptions<ChatDbContext> options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite(_chatConnection)
            .Options;
        return new ChatDbContext(options);
    }

    // A fresh AppIdentityDbContext bound to its OWN shared connection (see class remarks).
    public AppIdentityDbContext NewIdentityContext()
    {
        DbContextOptions<AppIdentityDbContext> options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseSqlite(_identityConnection)
            .Options;
        return new AppIdentityDbContext(options);
    }

    public void Dispose()
    {
        _chatConnection.Dispose();
        _identityConnection.Dispose();
    }
}
