using Rocket.API;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

internal class AddBattlepassCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "addbp";

    public string Help => "Add battlepass to a player";

    public string Syntax => "/addbp (SteamID)";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length == 0)
        {
            Utility.Say(caller, $"<color=red>Correct Usage: {Syntax}</color>");
            return;
        }

        if (!ulong.TryParse(command[0], out var steamid))
        {
            Utility.Say(caller, $"<color=red>SteamID is not in the correct format</color>");
            return;
        }

        CSteamID steamID = new(steamid);
        Plugin.Instance.DB.AddPlayerBattlepass(steamID);
        Plugin.Instance.UI.OnBattlepassUpdated(steamID);
        Utility.Say(caller, $"<color=green>Added battlepass to {steamID}</color>");
    }
}