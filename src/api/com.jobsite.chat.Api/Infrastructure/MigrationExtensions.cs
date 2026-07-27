using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Api.Infrastructure;

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
