using MessagePack;

namespace MyMMORPG.Shared.Messages.Player;

[MessagePackObject]
public sealed class GetPlayerReq
{
    [Key(0)]
    public long PlayerId { get; set; }
}
[MessagePackObject]
public sealed class GetPlayerResp
{
    [Key(0)]
    public PlayerDto Player { get; set; } = new();
}
