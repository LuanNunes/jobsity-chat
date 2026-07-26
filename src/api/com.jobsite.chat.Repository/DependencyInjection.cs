using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using com.jobsite.chat.Repository.Repositories;
using com.jobsite.chat.Shared.Contracts.Repositories;
using com.jobsite.chat.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace com.jobsite.chat.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(PersistenceKeys.ChatDatabaseCnnName)
            ?? throw new InvalidOperationException(
                $"Connection string '{PersistenceKeys.ChatDatabaseCnnName}' is not configured.");

        services.AddDbContext<ChatDbContext>(options =>
            options.UseChatSqlite(connectionString, PersistenceKeys.ChatMigrationsHistoryTable));
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseChatSqlite(connectionString, PersistenceKeys.IdentityMigrationsHistoryTable));

        services.AddScoped(typeof(IDataContext<>), typeof(DataContext<>));

        // Explicit factories pin the primary IDataContext ctor; both repos have two 1-arg
        // ctors, so open-generic registration would otherwise trip MS.DI's ambiguous-ctor rule.
        services.AddScoped<IChatRoomRepository>(sp =>
            new ChatRoomRepository(sp.GetRequiredService<IDataContext<ChatDbContext>>()));
        services.AddScoped<IChatMessageRepository>(sp =>
            new ChatMessageRepository(sp.GetRequiredService<IDataContext<ChatDbContext>>()));
        return services;
    }
}
