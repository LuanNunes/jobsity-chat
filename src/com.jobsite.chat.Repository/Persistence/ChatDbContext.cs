using com.jobsite.chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Persistence;

public sealed class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<ChatRoom> Rooms => Set<ChatRoom>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
}
