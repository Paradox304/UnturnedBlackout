using Rocket.API;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands;

public class CountryCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller
    {
        get
        {
            return AllowedCaller.Console;
        }
    }

    public string Name
    {
        get
        {
            return "country";
        }
    }

    public string Help
    {
        get
        {
            return "Override the country code of a player";
        }
    }

    public string Syntax
    {
        get
        {
            return "/country (SteamID) (CountryCode)";
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
        var countryCode = command[1].ToUpper();
        _ = Task.Run(async () => await Plugin.Instance.DB.UpdatePlayerCountryCodeAsync(steamID, countryCode));
    }
}
