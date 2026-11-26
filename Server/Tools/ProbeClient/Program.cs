using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.ProbeClient;

public static class Program
{
    private static readonly string Host =
        Environment.GetEnvironmentVariable("MYMMORPG_HOST") ?? "127.0.0.1";

    private static readonly int Port =
        int.TryParse(Environment.GetEnvironmentVariable("MYMMORPG_PORT"), out var p) ? p : 19222;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0)
        {
            PrintUsage();
            return 2;
        }

        var command = args[0].Trim().ToLowerInvariant();

        try
        {
            await using var client = await RpcClient.ConnectAsync(Host, Port);

            switch (command)
            {
                case "create":
                    return await RunCreateAsync(client, args);
                case "get":
                    return await RunGetAsync(client, args);
                case "update":
                    return await RunUpdateAsync(client, args);
                case "delete":
                    return await RunDeleteAsync(client, args);
                case "list":
                    return await RunListAsync(client, args);
                case "demo":
                    return await RunDemoAsync(client);
                default:
                    Console.Error.WriteLine($"未知命令: {command}");
                    PrintUsage();
                    return 2;
            }
        }
        catch (RpcException ex)
        {
            Console.Error.WriteLine($"业务错误 [{(int)ex.Code} {ex.Code}]: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"连接或协议错误: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunCreateAsync(RpcClient client, string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("用法: create <名字>");
            return 2;
        }

        var name = string.Join(' ', args.Skip(1));
        var response = await client.CallAsync<CreatePlayerReq, CreatePlayerResp>(
            NetMessageType.CreatePlayer, new CreatePlayerReq { Name = name });

        PrintPlayer("创建成功", response.Player);
        return 0;
    }

    private static async Task<int> RunGetAsync(RpcClient client, string[] args)
    {
        if (args.Length < 2 || !long.TryParse(args[1], out var playerId))
        {
            Console.Error.WriteLine("用法: get <playerId>");
            return 2;
        }

        var response = await client.CallAsync<GetPlayerReq, GetPlayerResp>(
            NetMessageType.GetPlayer, new GetPlayerReq { PlayerId = playerId });

        PrintPlayer("查询结果", response.Player);
        return 0;
    }

    private static async Task<int> RunUpdateAsync(RpcClient client, string[] args)
    {
        if (args.Length < 2 || !long.TryParse(args[1], out var playerId))
        {
            Console.Error.WriteLine("用法: update <playerId> [--level N] [--exp N] [--gold N]");
            return 2;
        }

        var request = new UpdatePlayerReq
        {
            PlayerId = playerId,
            Level = ReadIntOption(args, "--level"),
            Exp = ReadLongOption(args, "--exp"),
            Gold = ReadLongOption(args, "--gold"),
        };

        var response = await client.CallAsync<UpdatePlayerReq, UpdatePlayerResp>(
            NetMessageType.UpdatePlayer, request);

        PrintPlayer("更新成功", response.Player);
        return 0;
    }

    private static async Task<int> RunDeleteAsync(RpcClient client, string[] args)
    {
        if (args.Length < 2 || !long.TryParse(args[1], out var playerId))
        {
            Console.Error.WriteLine("用法: delete <playerId>");
            return 2;
        }

        var response = await client.CallAsync<DeletePlayerReq, DeletePlayerResp>(
            NetMessageType.DeletePlayer, new DeletePlayerReq { PlayerId = playerId });

        Console.WriteLine(response.Deleted ? "删除成功" : "删除失败：玩家不存在");
        return response.Deleted ? 0 : 1;
    }

    private static async Task<int> RunListAsync(RpcClient client, string[] args)
    {
        var limit = args.Length >= 2 && int.TryParse(args[1], out var value) ? value : 50;
        var response = await client.CallAsync<ListPlayersReq, ListPlayersResp>(
            NetMessageType.ListPlayers, new ListPlayersReq { Limit = limit });

        Console.WriteLine($"共 {response.Players.Count} 条：");
        foreach (var player in response.Players)
        {
            PrintPlayer("", player);
        }

        return 0;
    }

    private static async Task<int> RunDemoAsync(RpcClient client)
    {
        var name = $"Demo_{DateTime.UtcNow:HHmmss}_{Random.Shared.Next(1000, 9999)}";

        var created = await client.CallAsync<CreatePlayerReq, CreatePlayerResp>(
            NetMessageType.CreatePlayer, new CreatePlayerReq { Name = name });
        PrintPlayer("1) 创建", created.Player);

        var fetched = await client.CallAsync<GetPlayerReq, GetPlayerResp>(
            NetMessageType.GetPlayer, new GetPlayerReq { PlayerId = created.Player.PlayerId });
        PrintPlayer("2) 查询", fetched.Player);

        var updated = await client.CallAsync<UpdatePlayerReq, UpdatePlayerResp>(
            NetMessageType.UpdatePlayer,
            new UpdatePlayerReq
            {
                PlayerId = created.Player.PlayerId,
                Level = 10,
                Exp = 1234,
                Gold = 5678,
            });
        PrintPlayer("3) 更新", updated.Player);

        var list = await client.CallAsync<ListPlayersReq, ListPlayersResp>(
            NetMessageType.ListPlayers, new ListPlayersReq { Limit = 10 });
        Console.WriteLine($"4) 列表：{list.Players.Count} 条");

        var deleted = await client.CallAsync<DeletePlayerReq, DeletePlayerResp>(
            NetMessageType.DeletePlayer, new DeletePlayerReq { PlayerId = created.Player.PlayerId });
        Console.WriteLine($"5) 删除：{deleted.Deleted}");

        return 0;
    }

    private static void PrintPlayer(string title, PlayerDto player)
    {
        if (!string.IsNullOrEmpty(title))
        {
            Console.WriteLine(title);
        }

        Console.WriteLine($"  id={player.PlayerId} name={player.Name} level={player.Level} " +
                          $"exp={player.Exp} gold={player.Gold} updatedAt={player.UpdatedAt:O}");
    }

    private static int? ReadIntOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length)
        {
            return null;
        }

        return int.TryParse(args[index + 1], out var value) ? value : null;
    }

    private static long? ReadLongOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length)
        {
            return null;
        }

        return long.TryParse(args[index + 1], out var value) ? value : null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "MyMMORPG ProbeClient\n" +
            "用法:\n" +
            "  ProbeClient create <名字>\n" +
            "  ProbeClient get <playerId>\n" +
            "  ProbeClient update <playerId> [--level N] [--exp N] [--gold N]\n" +
            "  ProbeClient delete <playerId>\n" +
            "  ProbeClient list [limit]\n" +
            "  ProbeClient demo\n" +
            "环境变量:\n" +
            "  MYMMORPG_HOST  默认 127.0.0.1\n" +
            "  MYMMORPG_PORT  默认 19222");
    }
}
