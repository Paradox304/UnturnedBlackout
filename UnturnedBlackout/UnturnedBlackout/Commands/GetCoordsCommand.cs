using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

class GetCoordsCommand : IRocketCommand
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
            return "getcoords";
        }
    }

    public string Help
    {
        get
        {
            return "Get your coordinates";
        }
    }

    public string Syntax
    {
        get
        {
            return "/getcoords";
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
        Utility.Say(caller, $"X: {player.Position.x}, Y: {player.Position.z}, Z: {player.Position.z}");
    }
}
