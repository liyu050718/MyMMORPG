using MessagePack;
using System.Buffers.Binary;

namespace MyMMORPG.Shared.Protocol;
public sealed class NetPacket
{
    public const int HeaderSize = 16;//帧头固定长度（除了frameLength和payload）

    public const int MaxFrameBytesDefault = 1024 * 1024;//帧最大为1MB

    public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard;

    public int MessageType { get; init; }//消息类型

    public long RequestId { get; init; }//请求序号

    public int ErrorCode { get; init; }//错误码

    public byte[] Payload { get; init; } = Array.Empty<byte>();

    public static NetPacket Request<T>(int messageType, long requestId, T body) => new()
    {
        MessageType = messageType,
        RequestId = requestId,
        Payload = MessagePackSerializer.Serialize(body, Options),
    };

    public static NetPacket Error(NetPacket request, ErrorCode code, string message) => new()
    {
        MessageType = request.MessageType,
        RequestId = request.RequestId,
        ErrorCode = (int)code,
        Payload = MessagePackSerializer.Serialize(new RpcError { Message = message }, Options),
    };

    public T Deserialize<T>()
    => MessagePackSerializer.Deserialize<T>(Payload, Options);

    public static async Task<NetPacket?> ReadAsync(Stream stream, int maxFrameBytes, CancellationToken ct = default)
    {
        var lengthBuffer = new byte[4];
        int first = await stream.ReadAsync(lengthBuffer.AsMemory(0, 4), ct);
        if (first == 0)
        {
            return null;
        }
        int read = first;
        while (read < 4)
        {
            int n = await stream.ReadAsync(lengthBuffer.AsMemory(read, 4 - read), ct);
            if (n == 0)
            {
                throw new EndOfStreamException("读取帧长度时连接已关闭");
            }
            read += n;
        }

        var frameLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        if (frameLength < HeaderSize || frameLength > maxFrameBytes)
        {
            throw new InvalidDataException($"非法帧长度: {frameLength}，允许范围 {HeaderSize}~{maxFrameBytes}");
        }

        var frame = new byte[frameLength];
        await stream.ReadExactlyAsync(frame, ct);

        var messageType = BinaryPrimitives.ReadInt32LittleEndian(frame.AsSpan(0, 4));
        var requestId = BinaryPrimitives.ReadInt64LittleEndian(frame.AsSpan(4, 8));
        var errorCode = BinaryPrimitives.ReadInt32LittleEndian(frame.AsSpan(12, 4));
        var payload = frame.AsSpan(HeaderSize).ToArray();

        return new NetPacket
        {
            MessageType = messageType,
            RequestId = requestId,
            ErrorCode = errorCode,
            Payload = payload,
        };
    }

    public static async Task WriteAsync(Stream stream, NetPacket packet, CancellationToken ct = default)
    {
        if (packet.Payload.Length > MaxFrameBytesDefault)
        {
            throw new InvalidDataException("响应包超过 1 MiB");
        }

        var frameLength = HeaderSize + packet.Payload.Length;
        var buffer = new byte[4 + frameLength];

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), frameLength);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4, 4), packet.MessageType);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8, 8), packet.RequestId);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(16, 4), packet.ErrorCode);

        packet.Payload.CopyTo(buffer.AsSpan(20));
        await stream.WriteAsync(buffer, ct);

        await stream.FlushAsync(ct);

    }
}
