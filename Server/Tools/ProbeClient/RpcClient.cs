using System.Net.Sockets;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.ProbeClient;

/// <summary>
/// 最小 RPC 客户端：一个连接、请求串行、按 requestId 校验响应。
/// </summary>
internal sealed class RpcClient : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private long _requestId;

    private RpcClient(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    public static async Task<RpcClient> ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);
        client.NoDelay = true;
        return new RpcClient(client);
    }

    public async Task<TResponse> CallAsync<TRequest, TResponse>(
        int messageType, TRequest request, CancellationToken ct = default)
        where TRequest : class
        where TResponse : class
    {
        var requestId = Interlocked.Increment(ref _requestId);
        var packet = NetPacket.Request(messageType, requestId, request);

        await NetPacket.WriteAsync(_stream, packet, ct);
        var response = await NetPacket.ReadAsync(_stream, NetPacket.MaxFrameBytesDefault, ct)
                       ?? throw new IOException("服务器已关闭连接");

        if (response.RequestId != requestId)
        {
            throw new InvalidDataException(
                $"响应 requestId 不匹配：期望 {requestId}，实际 {response.RequestId}");
        }

        if (response.ErrorCode != (int)ErrorCode.Ok)
        {
            var error = response.Deserialize<RpcError>();
            throw new RpcException((ErrorCode)response.ErrorCode, error.Message);
        }

        return response.Deserialize<TResponse>();
    }

    public ValueTask DisposeAsync()
    {
        _stream.Dispose();
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
