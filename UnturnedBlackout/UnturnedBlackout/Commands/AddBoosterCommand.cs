using Rocket.API;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Commands;

class AddBoosterCommand : IRocketCommand
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
            return "addbooster";
        }
    }

    public string Help
    {
        get
        {
            return "Add a booster to a player";
        }
    }

    public string Syntax
    {
        get
        {
            return "/addbooster (SteamID) (BoosterType) (BoosterValue) (ExpirationDays)";
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
        if (command.Length < 4)
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

        if (!Enum.TryParse(command[1], true, out EBoosterType boosterType))
        {
            Utility.Say(caller, $"<color=red>Booster Type is not in the correct format</color>");
            return;
        }

        if (!float.TryParse(command[2], out var boosterValue))
        {
            Utility.Say(caller, $"<color=red>Booster Value is not in the correct format</color>");
            return;
        }

        if (!int.TryParse(command[3], out var expirationDays))
        {
            Utility.Say(caller, $"<color=red>Expiration Days is not in the correct format</color>");
            return;
        }

        _ = Task.Run(async () => await Plugin.Instance.DB.AddPlayerBoosterAsync(steamID, boosterType, boosterValue, expirationDays));

        Utility.Say(caller, $"<color=green>Added booster with type {boosterType}, value {boosterValue}, days {expirationDays} to {steamID}</color>");
    }
}
