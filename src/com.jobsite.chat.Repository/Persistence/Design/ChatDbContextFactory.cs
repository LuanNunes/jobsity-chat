using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace com.jobsite.chat.Repository.Persistence.Design;

public sealed class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ChatDbContext> builder = new();
        builder.UseSqlite("Data Source=jobsity-chat.db",
            sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Chat"));
        return new ChatDbContext(builder.Options);
    }
}
