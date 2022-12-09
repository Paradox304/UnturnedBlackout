using System.Collections.Generic;
using Rocket.API;
using Steamworks;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

public class AddCaseCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length != 3)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
            return;
        }

        if (!ulong.TryParse(command[0], out var steamid))
        {
            Utility.Say(caller, "[color=red]Steam ID is not in correct format[/color]");
            return;
        }

        var steamID = new CSteamID(steamid);
        if (!int.TryParse(command[1], out var caseID))
        {
            Utility.Say(caller, "[color=red]Case ID is not in correct format[/color]");
            return;
        }

        if (!int.TryParse(command[2], out var amount))
        {
            Utility.Say(caller, "[color=red]Amount is not in correct format[/color]");
            return;
        }

        if (!Plugin.Instance.DB.Cases.TryGetValue(caseID, out var @case))
        {
            Utility.Say(caller, $"[color=red]Case with ID {caseID} not found[/color]");
            return;
        }
        
        Plugin.Instance.DB.IncreasePlayerCase(steamID, caseID, amount);
        Utility.Say(caller, $"[color=green]Added {amount}x {@case.CaseName} to {steamID}[/color]");
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "addcase";
    public string Help => "Add cases to players";
    public string Syntax => "/addcase (SteamID) (CaseID) (Amount)";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}