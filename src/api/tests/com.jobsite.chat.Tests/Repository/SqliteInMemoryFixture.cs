using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Tests.Repository;

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

    public SqliteConnection ChatConnection => _chatConnection;

    public ChatDbContext NewChatContext()
    {
        DbContextOptions<ChatDbContext> options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite(_chatConnection)
            .Options;
        return new ChatDbContext(options);
    }

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
