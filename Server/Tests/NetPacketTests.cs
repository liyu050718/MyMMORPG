using System.Buffers.Binary;
using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;
using Xunit;

namespace MyMMORPG.Tests;

public sealed class NetPacketTests
{
    [Fact]
    public async Task Request_RoundTrip_Preserves_Header_And_Body()
    {
        var packet = NetPacket.Request(
            NetMessageType.CreatePlayer, 42, new CreatePlayerReq { Name = "Alice" });

        await using var stream = new MemoryStream();
        await NetPacket.WriteAsync(stream, packet);
        stream.Position = 0;

        var read = await NetPacket.ReadAsync(stream, NetPacket.MaxFrameBytesDefault);
        Assert.NotNull(read);
        Assert.Equal(NetMessageType.CreatePlayer, read!.MessageType);
        Assert.Equal(42, read.RequestId);
        Assert.Equal(0, read.ErrorCode);

        var request = read.Deserialize<CreatePlayerReq>();
        Assert.Equal("Alice", request.Name);
    }

    [Fact]
    public async Task Error_Packet_RoundTrip()
    {
        var request = NetPacket.Request(NetMessageType.GetPlayer, 7, new GetPlayerReq());
        var error = NetPacket.Error(request, ErrorCode.NotFound, "玩家不存在");

        await using var stream = new MemoryStream();
        await NetPacket.WriteAsync(stream, error);
        stream.Position = 0;

        var read = await NetPacket.ReadAsync(stream, NetPacket.MaxFrameBytesDefault);
        Assert.NotNull(read);
        Assert.Equal((int)ErrorCode.NotFound, read!.ErrorCode);
        Assert.Equal("玩家不存在", read.Deserialize<RpcError>().Message);
    }

    [Fact]
    public async Task Invalid_Frame_Length_Throws()
    {
        var buffer = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, 0);

        await using var stream = new MemoryStream(buffer);
        await Assert.ThrowsAsync<InvalidDataException>(
            () => NetPacket.ReadAsync(stream, NetPacket.MaxFrameBytesDefault));
    }
}
