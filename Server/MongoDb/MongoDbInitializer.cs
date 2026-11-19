using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MyMMORPG.MongoDb.Attributes;
using System.Reflection;

namespace MyMMORPG.MongoDb;

public sealed class MongoDbInitializer
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MongoDbInitializer> _logger;

    public MongoDbInitializer(MongoDbContext context, ILogger<MongoDbInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EnsureIndexesAsync<T>(CancellationToken ct = default) where T : class, IMongoEntity
    {
        var collection = _context.GetCollection<T>();
        var attributes = typeof(T).GetCustomAttributes<MongoIndexAttribute>(false).ToArray();
        if (attributes.Length == 0)
        {
            _logger.LogInformation("集合 {Collection} 未声明索引，跳过",
                collection.CollectionNamespace.CollectionName);
            return;
        }

        var models = attributes.Select(attribute =>
        {
            var keys = attribute.Descending
                ? Builders<T>.IndexKeys.Descending(attribute.FieldName)
                : Builders<T>.IndexKeys.Ascending(attribute.FieldName);

            var name = $"{(attribute.Unique ? "ux" : "ix")}_{collection.CollectionNamespace.CollectionName}_{attribute.FieldName}";
            return new CreateIndexModel<T>(
                keys,
                new CreateIndexOptions { Unique = attribute.Unique, Name = name });
        }).ToList();

        await collection.Indexes.CreateManyAsync(models, ct);

        _logger.LogInformation("集合 {Collection} 索引已确保: {Indexes}",
            collection.CollectionNamespace.CollectionName,
            string.Join(", ", models.Select(m => m.Options.Name)));
    }
}