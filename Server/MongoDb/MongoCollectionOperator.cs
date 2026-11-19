using MongoDB.Driver;
using System.Linq.Expressions;

namespace MyMMORPG.MongoDb;

/// <summary>
/// 基于 MongoDB.Driver 的通用仓储实现。
/// </summary>
public sealed class MongoCollectionOperator<T> : IMongoCollectionOperator<T>
    where T : class, IMongoEntity
{
    private readonly IMongoCollection<T> _collection;

    public MongoCollectionOperator(MongoDbContext context)
    {
        _collection = context.GetCollection<T>();
    }

    public async Task InsertAsync(T entity, CancellationToken ct = default)
    {
        try
        {
            await _collection.InsertOneAsync(entity, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            throw new MongoDuplicateKeyException($"唯一键冲突: {ex.WriteError.Message}", ex);
        }
    }

    public async Task<T?> FindByIdAsync(long id, CancellationToken ct = default)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, int limit = 100,
        CancellationToken ct = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        return await _collection.Find(predicate).Limit(limit).ToListAsync(ct);
    }

    public async Task<List<T>> FindLatestAsync(int limit, CancellationToken ct = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        return await _collection.Find(FilterDefinition<T>.Empty)
            .Sort(Builders<T>.Sort.Descending("_id"))
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        var count = await _collection.CountDocumentsAsync(predicate, new CountOptions { Limit = 1 }, ct);
        return count > 0;
    }

    public async Task<bool> ReplaceAsync(T entity, CancellationToken ct = default)
    {
        var result = await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: ct);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var result = await _collection.DeleteOneAsync(x => x.Id == id, ct);
        return result.DeletedCount > 0;
    }
}