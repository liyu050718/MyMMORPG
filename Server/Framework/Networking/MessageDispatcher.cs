using MessagePack;
using Microsoft.Extensions.Logging;
using MyMMORPG.Shared.Protocol;
using System.Diagnostics;

namespace MyMMORPG.Framework.Networking;

/// <summary>
/// 按 msgType 找到 Handler，反序列化请求，调用业务，再序列化响应。
/// </summary>
public sealed class MessageDispatcher
{
    private readonly Dictionary<int, IMessageHandler> _handlers;
    private readonly ILogger<MessageDispatcher> _logger;

    public MessageDispatcher(IEnumerable<IMessageHandler> handlers, ILogger<MessageDispatcher> logger)
    {
        _logger = logger;
        _handlers = new Dictionary<int, IMessageHandler>();

        foreach (var handler in handlers)
        {
            if (!_handlers.TryAdd(handler.MessageType, handler))
            {
                throw new InvalidOperationException($"重复注册消息类型: {handler.MessageType}");
            }
        }
    }

    public async Task<NetPacket> DispatchAsync(NetPacket packet, ClientSession session, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            if (!_handlers.TryGetValue(packet.MessageType, out var handler))
            {
                return NetPacket.Error(packet, ErrorCode.UnknownMessageType, $"未知消息类型: {packet.MessageType}");
            }

            object request;
            try
            {
                request = MessagePackSerializer.Deserialize(
                              handler.RequestType, packet.Payload, NetPacket.Options, ct)
                          ?? throw new InvalidOperationException("请求体为空");
            }
            catch (Exception ex) when (ex is MessagePackSerializationException or ArgumentException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "反序列化失败 msgType={MessageType} requestId={RequestId}",
                    packet.MessageType, packet.RequestId);
                return NetPacket.Error(packet, ErrorCode.InvalidArgument, "请求体格式错误");
            }

            var response = await handler.HandleAsync(request, session, ct);
            if (response is null)
            {
                return NetPacket.Error(packet, ErrorCode.Internal, "服务器内部错误");
            }

            return new NetPacket
            {
                MessageType = packet.MessageType,
                RequestId = packet.RequestId,
                Payload = MessagePackSerializer.Serialize(response.GetType(), response, NetPacket.Options, ct),
            };
        }
        catch (RpcException ex)
        {
            _logger.LogInformation("RPC 业务错误 msgType={MessageType} code={Code} message={Message}",
                packet.MessageType, ex.Code, ex.Message);
            return NetPacket.Error(packet, ex.Code, ex.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC 处理异常 msgType={MessageType} requestId={RequestId}",
                packet.MessageType, packet.RequestId);
            return NetPacket.Error(packet, ErrorCode.Internal, "服务器内部错误");
        }
        finally
        {
            _logger.LogDebug("RPC 完成 msgType={MessageType} requestId={RequestId} cost={Cost}ms",
                packet.MessageType, packet.RequestId,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}