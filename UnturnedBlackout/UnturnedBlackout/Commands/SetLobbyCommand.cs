using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

internal class SetLobbyCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "setlobby";

    public string Help => "Set the lobby for players to join at your position";

    public string Syntax => "/setlobby";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is UnturnedPlayer player)
        {
            var transform = player.Player.transform;
            Plugin.Instance.Config.Base.FileData.LobbySpawn = transform.position;
            Plugin.Instance.Config.Base.FileData.LobbyYaw = transform.eulerAngles.y;
        }

        Plugin.Instance.Config.Base.Save();
    }
}