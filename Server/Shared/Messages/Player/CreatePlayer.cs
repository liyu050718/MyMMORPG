using MessagePack;

namespace MyMMORPG.Shared.Messages.Player;

[MessagePackObject]
public sealed class CreatePlayerReq
{
    [Key(0)] public string Name { get; set; } = string.Empty;
}

[MessagePackObject]
public sealed class CreatePlayerResp
{
    [Key(0)] public PlayerDto Player { get; set; } = new();
}
