using Rocket.API;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands;

public class CountryCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Console;

    public string Name => "country";

    public string Help => "Override the country code of a player";

    public string Syntax => "/country (SteamID) (CountryCode)";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

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
