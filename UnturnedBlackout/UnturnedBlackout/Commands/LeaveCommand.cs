using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

internal class LeaveCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "leave";

    public string Help => "Leave the game going on";

    public string Syntax => "/leave";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return;
        
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(player);
        if (gPlayer.CurrentGame != null)
        {
            if (gPlayer.CurrentGame.GamePhase == EGamePhase.ENDING)
            {
                Utility.Say(player, "<color=red>Game is ending</color>");
                return;
            }
            
            Plugin.Instance.Game.RemovePlayerFromGame(player);
            return;
        }

        if (!gPlayer.StaffMode)
            return;

        gPlayer.StaffMode = false;
        player.VanishMode = false;
        player.Player.look.sendFreecamAllowed(false);
        player.Player.look.sendSpecStatsAllowed(false);
        player.Player.movement.sendPluginSpeedMultiplier(1f);
        Plugin.Instance.Game.SendPlayerToLobby(player);
    }
}