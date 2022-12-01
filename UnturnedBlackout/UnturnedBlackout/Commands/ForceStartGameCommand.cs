using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Commands;

public class ForceStartGameCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return;

        var gPly = Plugin.Instance.Game.GetGamePlayer(player.CSteamID);

        var game = gPly?.CurrentGame;
        if (game is not { GamePhase: EGamePhase.WAITING_FOR_PLAYERS })
            return;

        game.ForceStartGame();
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "start";
    public string Help => "Force start the game you're in";
    public string Syntax => "/start";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}