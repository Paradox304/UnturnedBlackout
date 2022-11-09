using System.Collections.Generic;
using Rocket.API;
using SDG.Unturned;
using Steamworks;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Commands;

public class MapCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length != 1)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        GamePlayer gPlayer = null;
        if (ulong.TryParse(command[0], out var steamid))
            gPlayer = Plugin.Instance.Game.GetGamePlayer(new CSteamID(steamid));
        else
        {
            var ply = PlayerTool.getPlayer(command[0]);
            if (ply != null)
                gPlayer = Plugin.Instance.Game.GetGamePlayer(ply);
        }

        if (gPlayer == null)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Player_Not_Found"));
            return;
        }

        var map = gPlayer.CurrentGame?.Location?.LocationName ?? "None";
        Utility.Say(caller, $"<color=green>Map: {map}</color>");
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "mapcheck";
    public string Help => "Check which map the player is in";
    public string Syntax => "/mapcheck (PlayerName)";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}