using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

class LeaveCommand : IRocketCommand
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
            return "leave";
        }
    }

    public string Help
    {
        get
        {
            return "Leave the game going on";
        }
    }

    public string Syntax
    {
        get
        {
            return "/leave";
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
        Plugin.Instance.Game.RemovePlayerFromGame(player);
    }
}
