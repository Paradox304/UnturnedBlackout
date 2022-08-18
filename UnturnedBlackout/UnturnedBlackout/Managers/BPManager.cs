using Rocket.Core.Utils;
using Steamworks;
using System.Collections.Generic;
using System.Threading;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers
{
    public class BPManager
    {
        public HashSet<CSteamID> PendingWork { get; set; }

        public DatabaseManager DB
        {
            get
            {
                return Plugin.Instance.DB;
            }
        }

        public ConfigManager Config
        {
            get
            {
                return Plugin.Instance.Config;
            }

        }
        public BPManager()
        {
            PendingWork = new();
        }

        public void ClaimReward(GamePlayer player, bool isTop, int tierID)
        {
            if (!DB.BattlepassTiersSearchByID.TryGetValue(tierID, out BattlepassTier tier))
            {
                Logging.Debug($"Error finding battlepass tier with id {tierID} for {player.Player.CharacterName}");
                return;
            }

            var bp = player.Data.Battlepass;
            if (isTop && bp.ClaimedFreeRewards.Contains(tierID) || (!isTop && bp.ClaimedPremiumRewards.Contains(tierID)))
            {
                Logging.Debug($"{player.Player.CharacterName} has already claimed the reward for IsTop {isTop} and selected {tierID}, why is the plugin asking again?");
                return;
            }

            if (PendingWork.Contains(player.SteamID))
            {
                Logging.Debug($"{player.Player.CharacterName} is spam clicking probably");
                return;
            }

            PendingWork.Add(player.SteamID);

            Reward reward;
            if (isTop)
            {
                reward = tier.FreeReward;
                bp.ClaimedFreeRewards.Add(tierID);
            }
            else
            {
                reward = tier.PremiumReward;
                bp.ClaimedPremiumRewards.Add(tierID);
            }

            Plugin.Instance.Reward.GiveReward(player.SteamID, new List<Reward> { reward });
            Plugin.Instance.UI.OnBattlepassTierUpdated(player.SteamID, tierID);

            PendingWork.Remove(player.SteamID);
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                if (isTop)
                    await DB.UpdatePlayerBPClaimedFreeRewardsAsync(player.SteamID);
                else
                    await DB.UpdatePlayerBPClaimedPremiumRewardsAsync(player.SteamID);
            });
        }

        public void SkipTier(GamePlayer player)
        {
            var bp = player.Data.Battlepass;
            if (!DB.BattlepassTiersSearchByID.ContainsKey(bp.CurrentTier + 1))
            {
                Logging.Debug($"{player.Player.CharacterName} has already reached the end of battlepass");
                return;
            }

            if (PendingWork.Contains(player.SteamID))
            {
                Logging.Debug($"{player.Player.CharacterName} is spam clicking probably");
                return;
            }

            PendingWork.Add(player.SteamID);

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                if (player.Data.Coins >= Config.Base.FileData.BattlepassTierSkipCost)
                {
                    var oldTier = bp.CurrentTier;
                    bp.CurrentTier += 1;
                    player.Data.Coins -= Config.Base.FileData.BattlepassTierSkipCost;

                    await DB.DecreasePlayerCoinsAsync(player.SteamID, Config.Base.FileData.BattlepassTierSkipCost);
                    await DB.UpdateBPTierAsync(player.SteamID, bp.CurrentTier);

                    PendingWork.Remove(player.SteamID);
                    TaskDispatcher.QueueOnMainThread(() =>
                    {
                        Plugin.Instance.UI.OnBattlepassUpdated(player.SteamID);
                        Plugin.Instance.UI.OnBattlepassTierUpdated(player.SteamID, oldTier);
                        Plugin.Instance.UI.OnBattlepassTierUpdated(player.SteamID, bp.CurrentTier);
                    });
                }
            });
        }
    }
}
