using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Persistence;

// Single source of the SQLite provider wiring (provider + per-context migrations-history table).
// Shared by runtime DI (AddRepositoryLayer) and design-time self-configuration (DbContext.OnConfiguring);
// only the connection-string source differs between the two — config at runtime, env var at design time.
internal static class ChatSqliteOptions
{
    public static DbContextOptionsBuilder UseChatSqlite(
        this DbContextOptionsBuilder builder,
        string connectionString,
        string migrationsHistoryTable) =>
        builder.UseSqlite(connectionString, sqlite => sqlite.MigrationsHistoryTable(migrationsHistoryTable));
}
