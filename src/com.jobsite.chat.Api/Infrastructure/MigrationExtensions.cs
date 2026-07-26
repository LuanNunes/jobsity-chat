using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Api.Infrastructure;

// Applies pending migrations for both contexts on startup. They share one SQLite file with
// separate history tables (configured in AddRepositoryLayer) -> safe and idempotent.
public static class MigrationExtensions
{
    public static async Task MigrateDatabasesAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        ChatDbContext chat = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        AppIdentityDbContext identity = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        await chat.Database.MigrateAsync();
        await identity.Database.MigrateAsync();
    }
}
