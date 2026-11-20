using MyMMORPG.Framework.Networking;
using MyMMORPG.GameServer.Services.Player;
using MyMMORPG.Shared.Messages.Player;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.GameServer.RpcHandlers.Player;

public sealed class CreatePlayerHandler : MessageHandler<CreatePlayerReq, CreatePlayerResp>
{
    private readonly PlayerService _service;

    public CreatePlayerHandler(PlayerService service) => _service = service;

    public override int MessageType => NetMessageType.CreatePlayer;

    public override async Task<CreatePlayerResp> HandleAsync(
        CreatePlayerReq request, ClientSession session, CancellationToken ct)
        => new() { Player = await _service.CreateAsync(request.Name, ct) };
}
