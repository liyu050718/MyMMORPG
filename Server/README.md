# MyMMORPG Server Demo

一个仿照 MMORPG 服务端 Demo：
TCP + MessagePack RPC + MongoDB 仓储 + 控制台探针客户端。

## 环境

- Windows 10/11
- .NET 8 SDK（`dotnet --list-sdks` 应看到 8.0.x）
- MongoDB Community Server 7.x，默认 `127.0.0.1:27017`
- 可选：MongoDB Shell（mongosh）、VS Code + C# Dev Kit

## 构建

```powershell
cd D:\MyMMORPG\Server
dotnet build Server.sln
```

## 启动服务端

```powershell
dotnet run --project Server
```

看到 `NetServer 监听 127.0.0.1:19222` 即成功。

## 探针客户端

```powershell
dotnet run --project Tools/ProbeClient -- demo
dotnet run --project Tools/ProbeClient -- create Alice
dotnet run --project Tools/ProbeClient -- get <playerId>
dotnet run --project Tools/ProbeClient -- update <playerId> --level 10 --exp 100 --gold 200
dotnet run --project Tools/ProbeClient -- delete <playerId>
dotnet run --project Tools/ProbeClient -- list 20
```

## 数据库

```powershell
mongosh "mongodb://127.0.0.1:27017/mymmorpg" --eval "db.player.find().pretty()"
mongosh "mongodb://127.0.0.1:27017/mymmorpg" --eval "db.player.getIndexes()"
```

## 测试

```powershell
dotnet test Server.sln
# 只跑不依赖 Mongo 的测试
dotnet test Server.sln --filter "Category!=Mongo"
```

也可以通过 `MONGODB_URI` 指定测试数据库连接串。

## 协议

帧格式（小端）：
`[4B frameLength][4B msgType][8B requestId][4B errorCode][payload]`

消息类型：1001 CreatePlayer、1002 GetPlayer、1003 UpdatePlayer、
1004 DeletePlayer、1005 ListPlayers。
