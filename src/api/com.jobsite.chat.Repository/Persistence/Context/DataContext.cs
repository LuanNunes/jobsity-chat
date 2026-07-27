using System.Data.Common;
using System.Linq.Expressions;
using com.jobsite.chat.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace com.jobsite.chat.Repository.Persistence.Context;

public sealed class DataContext<TContext>(TContext context) : IDataContext<TContext> where TContext : DbContext
{
    public IQueryable<T> GetEntities<T>(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null) where T : class
    {
        IQueryable<T> query = context.Set<T>().AsNoTracking();

        if (include is not null)
        {
            query = include(query);
        }

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query;
    }

    public Task<T?> GetById<T, TKey>(TKey id, CancellationToken ct = default)
        where T : class, IEntity<TKey> where TKey : notnull =>
        context.Set<T>().FindAsync([id], ct).AsTask();

    public IQueryable<T> FromSql<T>(string sql, params object[] parameters) where T : class =>
        context.Set<T>().FromSqlRaw(sql, parameters);

    public async Task Insert<T, TKey>(T entity, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull
    {

        context.Set<T>().Add(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task BulkInsert<T, TKey>(IReadOnlyCollection<T> entities, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull
    {
        if (entities.Count == 0)
        {
            return;
        }

        context.Set<T>().AddRange(entities);
        await context.SaveChangesAsync(ct);
    }

    public async Task BulkDelete<T, TKey>(IReadOnlyCollection<TKey> ids, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull
    {
        if (ids.Count == 0)
        {
            return;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(T), "e");
        MemberExpression idProperty = Expression.Property(parameter, nameof(IEntity<TKey>.Id));
        MethodCallExpression containsCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(TKey)],
            Expression.Constant(ids),
            idProperty);
        Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(containsCall, parameter);

        await context.Set<T>().Where(predicate).ExecuteDeleteAsync(ct);
    }

    public Task BulkDelete<T, TKey>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull =>
        context.Set<T>().Where(predicate).ExecuteDeleteAsync(ct);

    public Task BatchUpdate<T, TKey>(
        Expression<Func<T, bool>> predicate,
        Action<UpdateSettersBuilder<T>> update,
        CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull =>
        context.Set<T>().Where(predicate).ExecuteUpdateAsync(update, ct);

    public Task SetEntityState<T, TKey>(T entity, EntityState state)
        where T : class, IEntityValidator<TKey> where TKey : notnull
    {
        context.Entry(entity).State = state;
        return Task.CompletedTask;
    }

    public Task SetUpdateEntityState<T, TKey>(T entity)
        where T : class, IEntityValidator<TKey> where TKey : notnull =>
        SetEntityState<T, TKey>(entity, EntityState.Modified);

    public async Task RemoveEntity<T, TKey>(TKey id, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull
    {

        T? entity = await context.Set<T>().FindAsync([id], ct);

        if (entity is not null)
        {
            context.Set<T>().Remove(entity);
        }
    }

    public string GetConnectionString() =>
        context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string is configured on the context.");

    public DbCommand CreateCommand() =>
        context.Database.GetDbConnection().CreateCommand();

    public int SaveChanges() =>
        context.SaveChanges();

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
