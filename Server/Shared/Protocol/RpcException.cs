namespace MyMMORPG.Shared.Protocol;

public sealed class RpcException : Exception
{
    public ErrorCode Code { get; }

    public RpcException(ErrorCode code, string message) : base(message)
    {
        Code = code;
    }
}
