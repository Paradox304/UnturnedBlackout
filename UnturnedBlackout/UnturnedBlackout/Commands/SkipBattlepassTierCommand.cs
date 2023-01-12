using System.Collections.Generic;
using Rocket.API;
using Steamworks;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

internal class SkipBattlepassTierCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "skipbptier";

    public string Help => "Skip the battlepass tier of a player to a tier";

    public string Syntax => "/skipbptier (SteamID) (TierID)";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 2)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        if (!ulong.TryParse(command[0], out var steamid))
        {
            Utility.Say(caller, $"[color=red]SteamID is not in the correct format[/color]");
            return;
        }

        CSteamID steamID = new(steamid);

        if (!int.TryParse(command[1], out var tierID))
        {
            Utility.Say(caller, $"[color=red]TierID is not in the correct format[/color]");
            return;
        }

        if (!Plugin.Instance.DB.PlayerData.TryGetValue(steamID, out var playerData))
        {
            Utility.Say(caller, $"[color=red]Player data of player is not found[/color]");
            return;
        }

        var bp = playerData.Battlepass;
        if (bp.CurrentTier >= tierID)
        {
            Utility.Say(caller, $"[color=red]Player is already on this tier or of a higher tier[/color]");
            return;
        }
        
        
        Plugin.Instance.DB.UpdatePlayerBPTier(steamID, tierID);
        Utility.Say(caller, $"[color=green]Skipped battlepass tier of {steamID} to tier {tierID}[/color]");
    }
}