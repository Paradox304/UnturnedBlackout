using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

class GetNavMeshCommand : IRocketCommand
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
            return "getnavmesh";
        }
    }

    public string Help
    {
        get
        {
            return "Get the nav mesh of the area you're in";
        }
    }

    public string Syntax
    {
        get
        {
            return "/getnavmesh";
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
        if (LevelNavigation.tryGetNavigation(player.Position, out var nav))
        {
            UnturnedChat.Say(caller, $"Nav Mesh: {nav}");
        }
    }
}
