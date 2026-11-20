using MyMMORPG.Framework.Networking;
using MyMMORPG.GameServer.Services.Player;
using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.GameServer.RpcHandlers.Player;

public sealed class ListPlayersHandler : MessageHandler<ListPlayersReq, ListPlayersResp>
{
    private readonly PlayerService _service;

    public ListPlayersHandler(PlayerService service) => _service = service;

    public override int MessageType => NetMessageType.ListPlayers;

    public override async Task<ListPlayersResp> HandleAsync(
        ListPlayersReq request, ClientSession session, CancellationToken ct)
        => new() { Players = await _service.ListAsync(request.Limit, ct) };
}