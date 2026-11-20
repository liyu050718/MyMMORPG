using MyMMORPG.GameServer.Data;
using MyMMORPG.Shared.Messages.Player;

namespace MyMMORPG.GameServer.Mappings;

public static class PlayerMapper
{
    public static PlayerDto ToDto(this Player player)
        => new()
        {
            PlayerId = player.Id,
            Name = player.Name,
            Level = player.Level,
            Exp = player.Exp,
            Gold = player.Gold,
            CreatedAt = player.CreatedAt,
            UpdatedAt = player.UpdatedAt,
        };
}