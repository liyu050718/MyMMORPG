using MessagePack;

namespace MyMMORPG.Shared.Messages.Player;

[MessagePackObject]
public sealed class PlayerDto
{
    [Key(0)] public long PlayerId { get; set; }
    [Key(1)] public string Name { get; set; }
    [Key(2)] public int Level { get; set; }
    [Key(3)] public long Exp { get; set; }
    [Key(4)] public long Gold { get; set; }
    [Key(5)] public DateTime CreatedAt { get; set; }
    [Key(6)] public DateTime UpdatedAt { get; set; }
}