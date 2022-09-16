using Rocket.API;
using Steamworks;
using System.Collections.Generic;
using System.Threading;

namespace UnturnedBlackout.Commands
{
    class RemoveRewardCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "removereward";

        public string Help => "Remove rewards to a player";

        public string Syntax => "/removereward (SteamID) (RewardString)";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 2)
            {
                Utility.Say(caller, $"<color=red>Correct Usage: {Syntax}</color>");
                return;
            }

            if (!ulong.TryParse(command[0], out ulong steamid))
            {
                Utility.Say(caller, $"<color=red>SteamID is not in the correct format</color>");
                return;
            }

            var steamID = new CSteamID(steamid);
            var rewards = Utility.GetRewardsFromString(command[1]);
            Plugin.Instance.Reward.RemoveRewards(steamID, rewards);
        }
    }
}
