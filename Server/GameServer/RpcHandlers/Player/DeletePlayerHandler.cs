using MyMMORPG.Framework.Networking;
using MyMMORPG.GameServer.Services.Player;
using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.GameServer.RpcHandlers.Player;

public sealed class DeletePlayerHandler : MessageHandler<DeletePlayerReq, DeletePlayerResp>
{
    private readonly PlayerService _service;

    public DeletePlayerHandler(PlayerService service) => _service = service;

    public override int MessageType => NetMessageType.DeletePlayer;

    public override async Task<DeletePlayerResp> HandleAsync(
        DeletePlayerReq request, ClientSession session, CancellationToken ct)
        => new() { Deleted = await _service.DeleteAsync(request.PlayerId, ct) };
}