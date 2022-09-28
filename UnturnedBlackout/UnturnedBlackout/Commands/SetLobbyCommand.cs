using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

class SetLobbyCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller
    {
        get
        {
            return AllowedCaller.Player;
        }
    }

    public string Name
    {
        get
        {
            return "setlobby";
        }
    }

    public string Help
    {
        get
        {
            return "Set the lobby for players to join at your position";
        }
    }

    public string Syntax
    {
        get
        {
            return "/setlobby";
        }
    }

    public List<string> Aliases
    {
        get
        {
            return new();
        }
    }

    public List<string> Permissions
    {
        get
        {
            return new();
        }
    }

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = caller as UnturnedPlayer;

        Plugin.Instance.Config.Base.FileData.LobbySpawn = player.Player.transform.position;
        Plugin.Instance.Config.Base.FileData.LobbyYaw = player.Player.transform.eulerAngles.y;
        Plugin.Instance.Config.Base.Save();
    }
}
