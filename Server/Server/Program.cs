using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMMORPG.Framework.Configuration;
using MyMMORPG.Framework.Ids;
using MyMMORPG.Framework.Networking;
using MyMMORPG.GameServer.Bootstrap;
using MyMMORPG.GameServer.Data;
using MyMMORPG.GameServer.RpcHandlers.Player;
using MyMMORPG.GameServer.Services.Player;
using MyMMORPG.MongoDb;

namespace MyMMORPG.Server;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        ServerConfiguration.Initialize(args);

        var services = new ServiceCollection();

        services.AddSingleton(ServerConfiguration.AppLaunchSettings);
        services.AddSingleton(ServerConfiguration.DbSettings);
        services.AddSingleton(ServerConfiguration.NetworkSettings);
        services.AddSingleton(ServerConfiguration.LogSettings);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(ServerConfiguration.LogSettings.MinimumLevel);
        });

        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<MongoDbInitializer>();
        services.AddSingleton<IMongoCollectionOperator<Player>>(
            sp => new MongoCollectionOperator<Player>(sp.GetRequiredService<MongoDbContext>()));
        services.AddSingleton<IIdGenerator>(
            _ => new SnowflakeIdGenerator(ServerConfiguration.AppLaunchSettings.SnowflakeWorkerId));

        services.AddSingleton<PlayerService>();

        services.AddSingleton<IMessageHandler, CreatePlayerHandler>();
        services.AddSingleton<IMessageHandler, GetPlayerHandler>();
        services.AddSingleton<IMessageHandler, UpdatePlayerHandler>();
        services.AddSingleton<IMessageHandler, DeletePlayerHandler>();
        services.AddSingleton<IMessageHandler, ListPlayersHandler>();

        services.AddSingleton<MessageDispatcher>();
        services.AddSingleton<NetServer>();
        services.AddSingleton<GameServerHost>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Server");
        await using var host = provider.GetRequiredService<GameServerHost>();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await host.StartAsync(cts.Token);
            logger.LogInformation("按 Ctrl+C 停止服务");
            await host.WaitForShutdownAsync(cts.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "服务启动失败");
            return 1;
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
        }
    }
}