using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

class GetCoordsCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "getcoords";

    public string Help => "Get your coordinates";

    public string Syntax => "/getcoords";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = caller as UnturnedPlayer;
        Utility.Say(caller, $"X: {player.Position.x}, Y: {player.Position.z}, Z: {player.Position.z}");
    }
}
