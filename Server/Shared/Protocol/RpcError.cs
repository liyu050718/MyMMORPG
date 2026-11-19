using MessagePack;

namespace MyMMORPG.Shared.Protocol;

[MessagePackObject]
public sealed class RpcError
{
    [Key(0)]
    public string Message { get; set; } = string.Empty;
}
