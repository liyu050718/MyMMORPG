using System.Linq.Expressions;

namespace MyMMORPG.MongoDb;

public interface IMongoCollectionOperator<T> where T : class, IMongoEntity
{
    Task InsertAsync(T entity, CancellationToken ct = default);
    Task<T?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, int limit = 100, CancellationToken ct = default);
    Task<List<T>> FindLatestAsync(int limit, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> ReplaceAsync(T entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}