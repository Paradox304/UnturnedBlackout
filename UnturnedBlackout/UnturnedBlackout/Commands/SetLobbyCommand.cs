using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

class SetLobbyCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "setlobby";

    public string Help => "Set the lobby for players to join at your position";

    public string Syntax => "/setlobby";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = caller as UnturnedPlayer;

        Plugin.Instance.Config.Base.FileData.LobbySpawn = player.Player.transform.position;
        Plugin.Instance.Config.Base.FileData.LobbyYaw = player.Player.transform.eulerAngles.y;
        Plugin.Instance.Config.Base.Save();
    }
}
