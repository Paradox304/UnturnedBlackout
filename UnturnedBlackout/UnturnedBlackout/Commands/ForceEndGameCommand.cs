using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Commands;

public class ForceEndGameCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return;

        var gPly = Plugin.Instance.Game.GetGamePlayer(player.CSteamID);

        var game = gPly?.CurrentGame;
        if (game is not { GamePhase: EGamePhase.STARTED })
            return;

        game.ForceEndGame();
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "end";
    public string Help => "Force end the game you're in";
    public string Syntax => "/end";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}