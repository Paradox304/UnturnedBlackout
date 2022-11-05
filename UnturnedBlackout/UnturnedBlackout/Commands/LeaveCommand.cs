using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

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
            Plugin.Instance.Game.RemovePlayerFromGame(player);
            return;
        }

        if (!gPlayer.StaffMode)
            return;

        gPlayer.StaffMode = false;
        player.GodMode = false;
        player.VanishMode = false;
        
        Plugin.Instance.Game.SendPlayerToLobby(player);
    }
}