using MyMMORPG.Shared.Protocol;
using System.Net;
using System.Net.Sockets;

namespace MyMMORPG.Framework.Networking;

/// <summary>
/// 一个客户端 TCP 连接。发送时加锁，保证同一连接不会交叉写帧。
/// </summary>
public sealed class ClientSession : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private int _disposed;

    internal ClientSession(long sessionId, TcpClient client)
    {
        SessionId = sessionId;
        _client = client;
        _stream = client.GetStream();
        RemoteEndPoint = client.Client.RemoteEndPoint;
    }

    public long SessionId { get; }
    public EndPoint? RemoteEndPoint { get; }
    public NetworkStream Stream => _stream;

    public async ValueTask SendAsync(NetPacket packet, CancellationToken ct = default)
    {
        await _sendLock.WaitAsync(ct);
        try
        {
            await NetPacket.WriteAsync(_stream, packet, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        _sendLock.Dispose();
        _stream.Dispose();
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}