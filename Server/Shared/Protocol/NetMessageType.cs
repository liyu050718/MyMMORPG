namespace MyMMORPG.Shared.Protocol;

public static class NetMessageType
{
    public const int CreatePlayer = 1001;//创建玩家
    public const int GetPlayer = 1002;//查询玩家
    public const int UpdatePlayer = 1003;//更新玩家数据
    public const int DeletePlayer = 1004;//删除玩家数据
    public const int ListPlayers = 1005;//
}