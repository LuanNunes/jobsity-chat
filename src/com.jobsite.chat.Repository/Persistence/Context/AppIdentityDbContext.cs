using com.jobsite.chat.Domain.Identity;
using com.jobsite.chat.Shared.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Persistence.Context;

public sealed class AppIdentityDbContext : IdentityDbContext<ApplicationUser>
{
    // Runtime: DI supplies the options (connection string + history table via AddRepositoryLayer).
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }

    // Design-time (dotnet ef): EF instantiates via this parameterless ctor and OnConfiguring
    // self-configures from the environment — no design-time factory, no Api host boot.
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
        base.OnModelCreating(builder);   // MUST run first: builds AspNet* tables
        builder.Entity<ApplicationUser>().Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
    }
}
