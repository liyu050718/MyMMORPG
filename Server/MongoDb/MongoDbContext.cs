using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MyMMORPG.Framework.Configuration;
using MyMMORPG.MongoDb.Attributes;

namespace MyMMORPG.MongoDb;

public sealed class MongoDbContext : IDisposable
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbContext> _logger;

    public MongoDbContext(DbSettings settings, ILogger<MongoDbContext> logger)
    {
        _logger = logger;

        var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(settings.ServerSelectionTimeoutSeconds);

        _client = new MongoClient(clientSettings);
        _database = _client.GetDatabase(settings.Database);
        _logger.LogDebug("MongoDB 客户端已创建，数据库={Database}", settings.Database);
    }

    public IMongoCollection<T> GetCollection<T>() where T : class, IMongoEntity
    {
        var attribute = typeof(T).GetCustomAttributes(typeof(MongodbCollectionDefineAttribute), false)
            .OfType<MongodbCollectionDefineAttribute>()
            .FirstOrDefault();

        var collectionName = attribute?.CollectionName ?? typeof(T).Name.ToLowerInvariant();
        return _database.GetCollection<T>(collectionName);
    }

    public async Task PingAsync(CancellationToken ct = default)
    {
        await _client.GetDatabase("admin")
            .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
    }

    /// <summary>
    /// 仅供测试或本地重置使用。
    /// </summary>
    public async Task DropDatabaseAsync(CancellationToken ct = default)
    {
        await _client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName, ct);
    }

    public void Dispose()
    {
        (_client as IDisposable)?.Dispose();
    }
}