using System.Diagnostics;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Persistence.Context;

public sealed class ChatDbContext : DbContext
{
    // Runtime: DI supplies the options (connection string + history table via AddRepositoryLayer).
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    // Design-time (dotnet ef): EF instantiates via this parameterless ctor and OnConfiguring
    // self-configures from the environment — no design-time factory, no Api host boot.
    public ChatDbContext() { }

    public DbSet<ChatRoom> Rooms => Set<ChatRoom>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        optionsBuilder.UseChatSqlite(JobsityChatCnnString.FromEnvironment(), PersistenceKeys.ChatMigrationsHistoryTable);

#if DEBUG
        optionsBuilder
            .LogTo(x => Debug.WriteLine(x))
            .EnableSensitiveDataLogging();
#endif
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
}
