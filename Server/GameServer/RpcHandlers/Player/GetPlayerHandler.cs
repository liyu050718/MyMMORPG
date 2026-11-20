using MyMMORPG.Framework.Networking;
using MyMMORPG.GameServer.Services.Player;
using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.GameServer.RpcHandlers.Player;

public sealed class GetPlayerHandler : MessageHandler<GetPlayerReq, GetPlayerResp>
{
    private readonly PlayerService _service;

    public GetPlayerHandler(PlayerService service) => _service = service;

    public override int MessageType => NetMessageType.GetPlayer;

    public override async Task<GetPlayerResp> HandleAsync(
        GetPlayerReq request, ClientSession session, CancellationToken ct)
        => new() { Player = await _service.GetAsync(request.PlayerId, ct) };
}