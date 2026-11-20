namespace MyMMORPG.Framework.Networking;

/// <summary>
/// 泛型 Handler 基类：把 object 请求转成具体请求类型。
/// </summary>
public abstract class MessageHandler<TRequest, TResponse> : IMessageHandler
    where TRequest : class
{
    public abstract int MessageType { get; }
    public Type RequestType => typeof(TRequest);

    public abstract Task<TResponse> HandleAsync(TRequest request, ClientSession session, CancellationToken ct);

    async Task<object?> IMessageHandler.HandleAsync(object request, ClientSession session, CancellationToken ct)
        => await HandleAsync((TRequest)request, session, ct);
}