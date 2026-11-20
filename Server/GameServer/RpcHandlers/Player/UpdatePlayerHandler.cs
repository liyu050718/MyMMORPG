using MyMMORPG.Framework.Networking;
using MyMMORPG.GameServer.Services.Player;
using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.GameServer.RpcHandlers.Player;

public sealed class UpdatePlayerHandler : MessageHandler<UpdatePlayerReq, UpdatePlayerResp>
{
    private readonly PlayerService _service;

    public UpdatePlayerHandler(PlayerService service) => _service = service;

    public override int MessageType => NetMessageType.UpdatePlayer;

    public override async Task<UpdatePlayerResp> HandleAsync(
        UpdatePlayerReq request, ClientSession session, CancellationToken ct)
        => new()
        {
            Player = await _service.UpdateAsync(
                request.PlayerId, request.Level, request.Exp, request.Gold, ct),
        };
}