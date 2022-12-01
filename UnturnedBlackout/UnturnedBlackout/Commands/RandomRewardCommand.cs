using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

[UsedImplicitly]
public class RandomRewardCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 2)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        if (!ulong.TryParse(command[0], out var steamid))
        {
            Utility.Say(caller, "<color=red>Steam ID is not in correct format</color>");
            return;
        }

        var steamID = new CSteamID(steamid);
        var rewards = Utility.GetRewardsFromString(command[1]);
        if (rewards.Count == 0)
        {
            Utility.Say(caller, "<color=red>You have set no rewards</color>");
            return;
        }

        var randomReward = rewards[UnityEngine.Random.Range(0, rewards.Count)];
        Plugin.Instance.Reward.GiveRewards(steamID, new() { randomReward });
        if (!Enum.TryParse(randomReward.RewardType.ToString(), true, out ECurrency currency))
            return;
        
        if (Provider.clients.Exists(k => k.playerID.steamID == steamID))
            Utility.Say(UnturnedPlayer.FromCSteamID(steamID), $"You claimed your free daily pack and received {randomReward.RewardValue} {currency.ToFriendlyName()}");
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "randomreward";
    public string Help => "Gives a random reward to the player from the set rewards";
    public string Syntax => "/randomreward (ID) (Rewards)";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}