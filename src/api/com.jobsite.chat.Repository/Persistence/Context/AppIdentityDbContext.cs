using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Shared.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Persistence.Context;

public sealed class AppIdentityDbContext : IdentityDbContext<ApplicationUser>
{

    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }

    public AppIdentityDbContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseChatSqlite(
                JobsityChatCnnString.FromEnvironment(), PersistenceKeys.IdentityMigrationsHistoryTable);
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
    }
}
