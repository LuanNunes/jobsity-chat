using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace com.jobsite.chat.Repository.Persistence.Design;

public sealed class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
{
    public AppIdentityDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<AppIdentityDbContext> builder = new();
        builder.UseSqlite("Data Source=jobsity-chat.db",
            sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Identity"));
        return new AppIdentityDbContext(builder.Options);
    }
}
