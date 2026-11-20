using Microsoft.Extensions.Logging;
using MyMMORPG.Framework.Configuration;
using MyMMORPG.Shared.Protocol;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace MyMMORPG.Framework.Networking;

/// <summary>
/// TCP 服务端：接受连接、每连接一个读循环、串行处理该连接的请求。
/// </summary>
public sealed class NetServer : IAsyncDisposable
{
    private readonly NetworkSettings _settings;
    private readonly MessageDispatcher _dispatcher;
    private readonly ILogger<NetServer> _logger;
    private readonly ConcurrentDictionary<long, ClientSession> _sessions = new();
    private readonly ConcurrentDictionary<long, Task> _sessionTasks = new();

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private long _sessionId;
    private int _started;
    private int _stopped;

    public NetServer(NetworkSettings settings, MessageDispatcher dispatcher, ILogger<NetServer> logger)
    {
        _settings = settings;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public int BoundPort { get; private set; }

    public Task StartAsync(CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("NetServer 已启动");
        }

        var address = string.IsNullOrWhiteSpace(_settings.Host) || _settings.Host == "0.0.0.0"
            ? IPAddress.Any
            : IPAddress.Parse(_settings.Host);

        _listener = new TcpListener(address, _settings.Port);
        _listener.Start();
        BoundPort = ((IPEndPoint)_listener.LocalEndpoint).Port;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _acceptTask = Task.Run(() => AcceptLoopAsync(_cts.Token), CancellationToken.None);

        _logger.LogInformation("NetServer 监听 {Address}:{Port}", _settings.Host, BoundPort);
        return Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "接受连接失败");
                break;
            }

            if (client is null)
            {
                continue;
            }

            if (_sessions.Count >= _settings.MaxClient)
            {
                _logger.LogWarning("连接数已达上限 {MaxClient}，拒绝新连接", _settings.MaxClient);
                client.Dispose();
                continue;
            }

            var sessionId = Interlocked.Increment(ref _sessionId);
            var session = new ClientSession(sessionId, client);
            _sessions[sessionId] = session;
            _logger.LogInformation("客户端连接 sessionId={SessionId} remote={Remote}",
                sessionId, session.RemoteEndPoint);

            var task = Task.Run(() => RunSessionAsync(session, ct), CancellationToken.None);
            _sessionTasks[sessionId] = task;
        }
    }

    private async Task RunSessionAsync(ClientSession session, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var packet = await NetPacket.ReadAsync(session.Stream, _settings.MaxFrameBytes, ct);
                if (packet is null)
                {
                    break;
                }

                var response = await _dispatcher.DispatchAsync(packet, session, ct);
                await session.SendAsync(response, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (EndOfStreamException)
        {
            _logger.LogInformation("客户端断开 sessionId={SessionId}", session.SessionId);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "协议错误，关闭连接 sessionId={SessionId}", session.SessionId);
        }
        catch (IOException ex)
        {
            _logger.LogInformation(ex, "连接中断 sessionId={SessionId}", session.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会话异常 sessionId={SessionId}", session.SessionId);
        }
        finally
        {
            _sessions.TryRemove(session.SessionId, out _);
            _sessionTasks.TryRemove(session.SessionId, out _);
            await session.DisposeAsync();
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }

        foreach (var session in _sessions.Values)
        {
            try { await session.DisposeAsync(); } catch { }
        }
        _sessions.Clear();

        if (_acceptTask is not null)
        {
            try { await _acceptTask.WaitAsync(TimeSpan.FromSeconds(5), ct); }
            catch (TimeoutException) { _logger.LogWarning("等待接受循环退出超时"); }
            catch (OperationCanceledException) { }
        }

        var tasks = _sessionTasks.Values.ToArray();
        if (tasks.Length > 0)
        {
            try { await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5), ct); }
            catch (TimeoutException) { _logger.LogWarning("等待会话退出超时"); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.LogWarning(ex, "会话退出时出现异常"); }
        }

        _logger.LogInformation("NetServer 已停止");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }
}