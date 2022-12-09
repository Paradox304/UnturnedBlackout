using System.Collections.Generic;
using Rocket.API;
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
        
        
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "addcase";
    public string Help => "Add cases to players";
    public string Syntax => "/addcase (SteamID) (CaseID) (Amount)";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}