namespace MyMMORPG.Framework.Networking;

/// <summary>
/// 所有 RPC Handler 的非泛型接口，Dispatcher 只依赖它。
/// </summary>
public interface IMessageHandler
{
    int MessageType { get; }
    Type RequestType { get; }
    Task<object?> HandleAsync(object request, ClientSession session, CancellationToken ct);
}