using Microsoft.Extensions.Logging.Abstractions;
using MyMMORPG.Framework.Configuration;
using MyMMORPG.GameServer.Data;
using MyMMORPG.MongoDb;
using Xunit;

namespace MyMMORPG.Tests;

/// <summary>
/// 每个测试类一个随机数据库，结束后 DropDatabase，互不干扰。
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    public string DatabaseName { get; } =
        $"mymmorpg_test_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

    public DbSettings Settings { get; private set; } = null!;
    public MongoDbContext Context { get; private set; } = null!;
    public IMongoCollectionOperator<Player> Players { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://127.0.0.1:27017";

        Settings = new DbSettings
        {
            ConnectionString = connectionString,
            Database = DatabaseName,
            ServerSelectionTimeoutSeconds = 5,
        };

        Context = new MongoDbContext(Settings, NullLogger<MongoDbContext>.Instance);
        await Context.PingAsync();

        var initializer = new MongoDbInitializer(Context, NullLogger<MongoDbInitializer>.Instance);
        await initializer.EnsureIndexesAsync<Player>();

        Players = new MongoCollectionOperator<Player>(Context);
    }

    public async Task DisposeAsync()
    {
        await Context.DropDatabaseAsync();
        Context.Dispose();
    }
}
