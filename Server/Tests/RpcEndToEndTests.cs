using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using MyMMORPG.Framework.Configuration;
using MyMMORPG.Framework.Ids;
using MyMMORPG.Framework.Networking;
using MyMMORPG.GameServer.RpcHandlers.Player;
using MyMMORPG.GameServer.Services.Player;
using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;
using Xunit;

namespace MyMMORPG.Tests;

[Trait("Category", "Mongo")]
public sealed class RpcEndToEndTests : IClassFixture<MongoFixture>, IAsyncLifetime
{
    private readonly MongoFixture _fixture;
    private NetServer _server = null!;
    private int _port;

    public RpcEndToEndTests(MongoFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        var service = new PlayerService(
            _fixture.Players,
            new SnowflakeIdGenerator(1),
            NullLogger<PlayerService>.Instance);

        IMessageHandler[] handlers =
        {
            new CreatePlayerHandler(service),
            new GetPlayerHandler(service),
            new UpdatePlayerHandler(service),
            new DeletePlayerHandler(service),
            new ListPlayersHandler(service),
        };

        var dispatcher = new MessageDispatcher(handlers, NullLogger<MessageDispatcher>.Instance);
        _server = new NetServer(
            new NetworkSettings
            {
                Host = "127.0.0.1",
                Port = 0,
                MaxClient = 10,
                MaxFrameBytes = NetPacket.MaxFrameBytesDefault,
            },
            dispatcher,
            NullLogger<NetServer>.Instance);

        await _server.StartAsync();
        _port = _server.BoundPort;
    }

    public async Task DisposeAsync()
    {
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task Tcp_Crud_Over_Real_Socket_Works()
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, _port);
        var stream = client.GetStream();

        var name = $"e2e_{Guid.NewGuid():N}"[..16];
        var createRequest = NetPacket.Request(
            NetMessageType.CreatePlayer, 1, new CreatePlayerReq { Name = name });
        var createResponse = await CallAsync(stream, createRequest);
        var created = createResponse.Deserialize<CreatePlayerResp>();
        Assert.True(created.Player.PlayerId > 0);

        var getResponse = await CallAsync(stream, NetPacket.Request(
            NetMessageType.GetPlayer, 2,
            new GetPlayerReq { PlayerId = created.Player.PlayerId }));
        Assert.Equal(name, getResponse.Deserialize<GetPlayerResp>().Player.Name);

        var updateResponse = await CallAsync(stream, NetPacket.Request(
            NetMessageType.UpdatePlayer, 3,
            new UpdatePlayerReq { PlayerId = created.Player.PlayerId, Level = 7 }));
        Assert.Equal(7, updateResponse.Deserialize<UpdatePlayerResp>().Player.Level);

        var listResponse = await CallAsync(stream, NetPacket.Request(
            NetMessageType.ListPlayers, 4, new ListPlayersReq { Limit = 10 }));
        Assert.Contains(
            listResponse.Deserialize<ListPlayersResp>().Players,
            p => p.PlayerId == created.Player.PlayerId);

        var deleteResponse = await CallAsync(stream, NetPacket.Request(
            NetMessageType.DeletePlayer, 5,
            new DeletePlayerReq { PlayerId = created.Player.PlayerId }));
        Assert.True(deleteResponse.Deserialize<DeletePlayerResp>().Deleted);

        var missing = await CallRawAsync(stream, NetPacket.Request(
            NetMessageType.GetPlayer, 6,
            new GetPlayerReq { PlayerId = created.Player.PlayerId }));
        Assert.Equal((int)ErrorCode.NotFound, missing.ErrorCode);
    }

    private static async Task<NetPacket> CallAsync(NetworkStream stream, NetPacket request)
    {
        var response = await CallRawAsync(stream, request);
        Assert.Equal((int)ErrorCode.Ok, response.ErrorCode);
        return response;
    }

    private static async Task<NetPacket> CallRawAsync(NetworkStream stream, NetPacket request)
    {
        await NetPacket.WriteAsync(stream, request);
        return await NetPacket.ReadAsync(stream, NetPacket.MaxFrameBytesDefault)
               ?? throw new IOException("服务器关闭连接");
    }
}
