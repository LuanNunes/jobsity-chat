using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Persistence;

internal static class ChatSqliteOptions
{
    public static DbContextOptionsBuilder UseChatSqlite(
        this DbContextOptionsBuilder builder,
        string connectionString,
        string migrationsHistoryTable) =>
        builder.UseSqlite(connectionString, sqlite => sqlite.MigrationsHistoryTable(migrationsHistoryTable));
}
