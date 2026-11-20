using Microsoft.Extensions.Logging;
using MyMMORPG.Framework.Networking;
using MyMMORPG.GameServer.Data;
using MyMMORPG.MongoDb;

namespace MyMMORPG.GameServer.Bootstrap;

/// <summary>
/// 组合根之后的启动编排：连 Mongo、建索引、开网络、等待退出。
/// </summary>
public sealed class GameServerHost : IAsyncDisposable
{
    private const int MongoConnectAttempts = 3;
    private static readonly TimeSpan MongoRetryDelay = TimeSpan.FromSeconds(2);

    private readonly MongoDbContext _mongo;
    private readonly MongoDbInitializer _initializer;
    private readonly NetServer _server;
    private readonly ILogger<GameServerHost> _logger;

    private int _started;
    private int _stopped;

    public GameServerHost(
        MongoDbContext mongo,
        MongoDbInitializer initializer,
        NetServer server,
        ILogger<GameServerHost> logger)
    {
        _mongo = mongo;
        _initializer = initializer;
        _server = server;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("GameServerHost 已启动");
        }

        await ConnectMongoAsync(ct);
        await _initializer.EnsureIndexesAsync<Player>(ct);
        await _server.StartAsync(ct);

        _logger.LogInformation("GameServer 启动完成，等待客户端连接");
    }

    private async Task ConnectMongoAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MongoConnectAttempts; attempt++)
        {
            try
            {
                await _mongo.PingAsync(ct);
                _logger.LogInformation("MongoDB 连接成功（第 {Attempt} 次尝试）", attempt);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MongoConnectAttempts)
            {
                _logger.LogWarning(ex,
                    "MongoDB 连接失败（第 {Attempt}/{Max} 次），{Delay} 秒后重试",
                    attempt, MongoConnectAttempts, MongoRetryDelay.TotalSeconds);
                await Task.Delay(MongoRetryDelay, ct);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "无法连接 MongoDB，请确认 mongod 已启动且 db.json 的连接串正确", ex);
            }
        }
    }

    public Task WaitForShutdownAsync(CancellationToken ct)
        => Task.Delay(Timeout.InfiniteTimeSpan, ct);

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        await _server.StopAsync(ct);
        _logger.LogInformation("GameServer 已停止");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _mongo.Dispose();
    }
}