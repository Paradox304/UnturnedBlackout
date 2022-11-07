using System.Collections.Generic;
using Rocket.API;

namespace UnturnedBlackout.Commands;

public class MapCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length != 1)
        {
            
        }
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "map";
    public string Help => "Check which map the player is in";
    public string Syntax => "/map (PlayerName)";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}