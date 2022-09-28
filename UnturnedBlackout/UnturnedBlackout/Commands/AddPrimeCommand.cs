using Rocket.API;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands;

class AddPrimeCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller
    {
        get
        {
            return AllowedCaller.Both;
        }
    }

    public string Name
    {
        get
        {
            return "addprime";
        }
    }

    public string Help
    {
        get
        {
            return "Add prime to a player";
        }
    }

    public string Syntax
    {
        get
        {
            return "/addprime (SteamID) (Days)";
        }
    }

    public List<string> Aliases
    {
        get
        {
            return new();
        }
    }

    public List<string> Permissions
    {
        get
        {
            return new();
        }
    }

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 2)
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

        if (!int.TryParse(command[1].ToString(), out var days))
        {
            Utility.Say(caller, $"<color=red>Days is not in the correct format</color>");
            return;
        }

        _ = Task.Run(async () => await Plugin.Instance.DB.AddPlayerPrimeAsync(steamID, days));

        Utility.Say(caller, $"<color=green>Added prime with days {days} to {steamID}</color>");
    }
}
