using MessagePack;

namespace MyMMORPG.Shared.Messages.Player;

[MessagePackObject]
public sealed class UpdatePlayerReq
{
    [Key(0)] public long PlayerId { get; set; }
    [Key(1)] public int? Level { get; set; }
    [Key(2)] public long? Exp { get; set; }
    [Key(3)] public long? Gold { get; set; }
}
[MessagePackObject]
public sealed class UpdatePlayerResp
{
    [Key(0)] public PlayerDto Player { get; set; } = new();
}