namespace com.jobsite.chat.Shared.Infrastructure;

// Central keys/names for the persistence layer — no magic strings across the DbContexts, the
// design-time factories, and DI. At runtime the connection string comes from configuration; at
// design time (dotnet ef) it comes from an environment variable.
public static class PersistenceKeys
{
    // Name of the connection string in configuration (ConnectionStrings:ChatDatabase).
    public const string ChatDatabaseCnnName = "ChatDatabase";

    // Environment variable holding the full connection string for design-time migrations (dotnet ef).
    public const string JobsityChatCnnString = "JOBSITY_CHAT_CONNECTION_STRING";

    // Separate migrations-history tables let both contexts share one database.
    public const string ChatMigrationsHistoryTable = "__EFMigrationsHistory_Chat";
    public const string IdentityMigrationsHistoryTable = "__EFMigrationsHistory_Identity";
}
