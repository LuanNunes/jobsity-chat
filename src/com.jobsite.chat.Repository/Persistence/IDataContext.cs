using System.Data.Common;
using System.Linq.Expressions;
using com.jobsite.chat.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace com.jobsite.chat.Repository.Persistence;

// EF-coupled data-access wrapper (spec §3.2). Deliberately lives in Repository, not Shared:
// its surface exposes EF types (DbContext, EntityState, IIncludableQueryable, DbCommand,
// UpdateSettersBuilder<T>).
public interface IDataContext<TContext> where TContext : DbContext
{
    IQueryable<T> GetEntities<T>(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null) where T : class;

    Task<T?> GetById<T, TKey>(TKey id, CancellationToken ct = default)
        where T : class, IEntity<TKey> where TKey : notnull;

    IQueryable<T> FromSql<T>(string sql, params object[] parameters) where T : class;

    Task Insert<T, TKey>(T entity, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull;

    Task BulkInsert<T, TKey>(IReadOnlyCollection<T> entities, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull;

    Task BulkDelete<T, TKey>(IReadOnlyCollection<TKey> ids, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull;

    Task BulkDelete<T, TKey>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull;

    Task BatchUpdate<T, TKey>(
        Expression<Func<T, bool>> predicate,
        Action<UpdateSettersBuilder<T>> update,
        CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull;

    Task SetEntityState<T, TKey>(T entity, EntityState state)
        where T : class, IEntityValidator<TKey> where TKey : notnull;

    Task SetUpdateEntityState<T, TKey>(T entity)
        where T : class, IEntityValidator<TKey> where TKey : notnull;

    Task RemoveEntity<T, TKey>(TKey id, CancellationToken ct = default)
        where T : class, IEntityValidator<TKey> where TKey : notnull;

    string GetConnectionString();
    DbCommand CreateCommand();
    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
