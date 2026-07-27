using System.Diagnostics;
using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Persistence.Context;

public sealed class ChatDbContext : DbContext
{

    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

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
