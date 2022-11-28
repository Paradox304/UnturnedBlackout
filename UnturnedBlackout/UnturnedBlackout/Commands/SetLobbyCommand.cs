using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

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
        if (caller is not UnturnedPlayer player)
            return;
        
        var transform = player.Player.transform.position;
        var spawn = new LobbySpawn(transform.x, transform.y, transform.z, player.Rotation);
        Plugin.Instance.Config.Base.FileData.LobbySpawns.Add(spawn);
        Plugin.Instance.Config.Base.Save();
    }
}