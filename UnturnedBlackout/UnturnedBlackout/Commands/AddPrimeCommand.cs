﻿using Rocket.API;
using Steamworks;
using System.Collections.Generic;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

internal class AddPrimeCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "addprime";

    public string Help => "Add prime to a player";

    public string Syntax => "/addprime (SteamID) (Days)";

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

        if (!int.TryParse(command[1], out var days))
        {
            Utility.Say(caller, $"[color=red]Days is not in the correct format[/color]");
            return;
        }

        Plugin.Instance.DB.AddPlayerPrime(steamID, days);
        Utility.Say(caller, $"[color=green]Added prime with days {days} to {steamID}[/color]");
    }
}