using MessagePack;

namespace MyMMORPG.Shared.Messages.Player;

[MessagePackObject]
public sealed class DeletePlayerReq
{
    [Key(0)] public long PlayerId { get; set; }
}

[MessagePackObject]
public sealed class DeletePlayerResp
{
    [Key(0)] public bool Deleted { get; set; }
}