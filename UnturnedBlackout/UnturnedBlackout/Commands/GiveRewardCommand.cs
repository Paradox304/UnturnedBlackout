using Rocket.API;
using Steamworks;
using System.Collections.Generic;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

internal class GiveRewardCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "givereward";

    public string Help => "Give rewards to a player";

    public string Syntax => "/givereward (SteamID) (RewardString)";

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
        var rewards = Utility.GetRewardsFromString(command[1]);
        Plugin.Instance.Reward.GiveRewards(steamID, rewards);
    }
}