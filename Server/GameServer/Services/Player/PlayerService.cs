using Microsoft.Extensions.Logging;
using MyMMORPG.Framework.Ids;
using MyMMORPG.GameServer.Data;
using PlayerEntity = MyMMORPG.GameServer.Data.Player;
using MyMMORPG.GameServer.Mappings;
using MyMMORPG.MongoDb;
using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.GameServer.Services.Player;

/// <summary>
/// 玩家业务：校验、ID 生成、持久化编排。
/// Handler 保持薄，所有规则都在这里。
/// </summary>
public sealed class PlayerService
{
    private const int MaxNameLength = 16;
    private const int MaxLevel = 100;
    private const int DefaultListLimit = 50;
    private const int MaxListLimit = 100;

    private readonly IMongoCollectionOperator<PlayerEntity> _players;
    private readonly IIdGenerator _ids;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(
        IMongoCollectionOperator<PlayerEntity> players,
        IIdGenerator ids,
        ILogger<PlayerService> logger)
    {
        _players = players;
        _ids = ids;
        _logger = logger;
    }

    public async Task<PlayerDto> CreateAsync(string? name, CancellationToken ct = default)
    {
        var normalized = NormalizeName(name);
        if (await _players.ExistsAsync(p => p.Name == normalized, ct))
        {
            throw new RpcException(ErrorCode.AlreadyExists, "玩家名已存在");
        }

        var now = DateTime.UtcNow;
        var player = new PlayerEntity
        {
            Id = _ids.NextId(),
            Name = normalized,
            Level = 1,
            Exp = 0,
            Gold = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            await _players.InsertAsync(player, ct);
        }
        catch (MongoDuplicateKeyException)
        {
            throw new RpcException(ErrorCode.AlreadyExists, "玩家名已存在");
        }

        _logger.LogInformation("创建玩家 {PlayerId} {Name}", player.Id, player.Name);
        return player.ToDto();
    }

    public async Task<PlayerDto> GetAsync(long playerId, CancellationToken ct = default)
    {
        if (playerId <= 0)
        {
            throw new RpcException(ErrorCode.InvalidArgument, "PlayerId 必须大于 0");
        }

        var player = await _players.FindByIdAsync(playerId, ct);
        if (player is null)
        {
            throw new RpcException(ErrorCode.NotFound, "玩家不存在");
        }

        return player.ToDto();
    }

    public async Task<PlayerDto> UpdateAsync(long playerId, int? level, long? exp, long? gold,
        CancellationToken ct = default)
    {
        if (playerId <= 0)
        {
            throw new RpcException(ErrorCode.InvalidArgument, "PlayerId 必须大于 0");
        }

        if (level is null && exp is null && gold is null)
        {
            throw new RpcException(ErrorCode.InvalidArgument, "至少提供一个要更新的字段");
        }

        if (level is < 1 or > MaxLevel)
        {
            throw new RpcException(ErrorCode.InvalidArgument, $"Level 必须在 1~{MaxLevel} 之间");
        }

        if (exp.HasValue && exp.Value < 0)
        {
            throw new RpcException(ErrorCode.InvalidArgument, "Exp 不能为负数");
        }

        if (gold.HasValue && gold.Value < 0)
        {
            throw new RpcException(ErrorCode.InvalidArgument, "Gold 不能为负数");
        }

        var player = await _players.FindByIdAsync(playerId, ct);
        if (player is null)
        {
            throw new RpcException(ErrorCode.NotFound, "玩家不存在");
        }

        if (level.HasValue) player.Level = level.Value;
        if (exp.HasValue) player.Exp = exp.Value;
        if (gold.HasValue) player.Gold = gold.Value;
        player.UpdatedAt = DateTime.UtcNow;

        if (!await _players.ReplaceAsync(player, ct))
        {
            throw new RpcException(ErrorCode.NotFound, "玩家不存在或已被删除");
        }

        _logger.LogInformation("更新玩家 {PlayerId}", playerId);
        return player.ToDto();
    }

    public async Task<bool> DeleteAsync(long playerId, CancellationToken ct = default)
    {
        if (playerId <= 0)
        {
            throw new RpcException(ErrorCode.InvalidArgument, "PlayerId 必须大于 0");
        }

        if (!await _players.DeleteAsync(playerId, ct))
        {
            throw new RpcException(ErrorCode.NotFound, "玩家不存在");
        }

        _logger.LogInformation("删除玩家 {PlayerId}", playerId);
        return true;
    }

    public async Task<List<PlayerDto>> ListAsync(int limit, CancellationToken ct = default)
    {
        var actualLimit = limit <= 0 ? DefaultListLimit : Math.Min(limit, MaxListLimit);
        var players = await _players.FindLatestAsync(actualLimit, ct);
        return players.Select(p => p.ToDto()).ToList();
    }

    private static string NormalizeName(string? name)
    {
        var normalized = name?.Trim() ?? string.Empty;

        if (normalized.Length is < 1 or > MaxNameLength)
        {
            throw new RpcException(ErrorCode.InvalidArgument,
                $"名字长度必须在 1~{MaxNameLength} 个字符之间");
        }

        if (normalized.Any(char.IsControl))
        {
            throw new RpcException(ErrorCode.InvalidArgument, "名字不能包含控制字符");
        }

        return normalized;
    }
}