using Rocket.API;
using Steamworks;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

class GiveRewardCommand : IRocketCommand
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
            return "givereward";
        }
    }

    public string Help
    {
        get
        {
            return "Give rewards to a player";
        }
    }

    public string Syntax
    {
        get
        {
            return "/givereward (SteamID) (RewardString)";
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
        var rewards = Utility.GetRewardsFromString(command[1]);
        Plugin.Instance.Reward.GiveRewards(steamID, rewards);
    }
}
