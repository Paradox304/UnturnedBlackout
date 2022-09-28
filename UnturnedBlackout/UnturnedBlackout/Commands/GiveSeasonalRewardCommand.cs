using Rocket.API;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

class GiveSeasonalRewardCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller
    {
        get
        {
            return AllowedCaller.Player;
        }
    }

    public string Name
    {
        get
        {
            return "giveseasonalreward";
        }
    }

    public string Help
    {
        get
        {
            return "Giving out seasonal rewards";
        }
    }

    public string Syntax
    {
        get
        {
            return "/giveseasonalreward";
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

    public void Execute(IRocketPlayer caller, string[] command) => Plugin.Instance.DB.IsPendingSeasonalWipe = true;
}
