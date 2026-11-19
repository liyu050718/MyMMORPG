using MessagePack;

namespace MyMMORPG.Shared.Messages.Player;

[MessagePackObject]
public sealed class ListPlayersReq
{
    [Key(0)] public int Limit { get; set; }
}

[MessagePackObject]
public sealed class ListPlayersResp
{
    [Key(0)] public List<PlayerDto> Players { get; set; } = new();
}