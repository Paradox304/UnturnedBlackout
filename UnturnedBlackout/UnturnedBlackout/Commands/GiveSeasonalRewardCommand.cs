﻿using Rocket.API;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

internal class GiveSeasonalRewardCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "giveseasonalreward";

    public string Help => "Giving out seasonal rewards";

    public string Syntax => "/giveseasonalreward";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command) => Plugin.Instance.DB.IsPendingSeasonalWipe = true;
}