using Rocket.Core.Utils;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers;

public class BPManager
{
    public HashSet<CSteamID> PendingWork { get; set; }

    public DatabaseManager DB => Plugin.Instance.DB;

    public ConfigManager Config => Plugin.Instance.Config;

    public BPManager()
    {
        PendingWork = new();
    }

    public void ClaimReward(GamePlayer player, bool isTop, int tierID)
    {
        if (!DB.BattlepassTiersSearchByID.TryGetValue(tierID, out var tier))
        {
            Logging.Debug($"Error finding battlepass tier with id {tierID} for {player.Player.CharacterName}");
            return;
        }

        var bp = player.Data.Battlepass;
        if ((isTop && bp.ClaimedFreeRewards.Contains(tierID)) || (!isTop && bp.ClaimedPremiumRewards.Contains(tierID)))
        {
            Logging.Debug($"{player.Player.CharacterName} has already claimed the reward for IsTop {isTop} and selected {tierID}, why is the plugin asking again?");
            return;
        }

        if (PendingWork.Contains(player.SteamID))
        {
            Logging.Debug($"{player.Player.CharacterName} is spam clicking probably");
            return;
        }

        _ = PendingWork.Add(player.SteamID);

        Reward reward;
        if (isTop)
        {
            reward = tier.FreeReward;
            _ = bp.ClaimedFreeRewards.Add(tierID);
        }
        else
        {
            reward = tier.PremiumReward;
            _ = bp.ClaimedPremiumRewards.Add(tierID);
        }

        Plugin.Instance.Reward.GiveRewards(player.SteamID, new() { reward });
        Plugin.Instance.UI.OnBattlepassTierUpdated(player.SteamID, tierID);

        _ = PendingWork.Remove(player.SteamID);

        if (isTop)
            DB.UpdatePlayerBPClaimedFreeRewards(player.SteamID);
        else
            DB.UpdatePlayerBPClaimedPremiumRewards(player.SteamID);
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

        _ = PendingWork.Add(player.SteamID);

        if (player.Data.Coins >= Config.Base.FileData.BattlepassTierSkipCost)
        {
            var oldTier = bp.CurrentTier;
            bp.CurrentTier += 1;
            
            DB.DecreasePlayerCoins(player.SteamID, Config.Base.FileData.BattlepassTierSkipCost);
            DB.UpdatePlayerBPTier(player.SteamID, bp.CurrentTier);

            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UI.OnBattlepassUpdated(player.SteamID);
                Plugin.Instance.UI.OnBattlepassTierUpdated(player.SteamID, oldTier);
                Plugin.Instance.UI.OnBattlepassTierUpdated(player.SteamID, bp.CurrentTier);
            });
        }
        else
            Plugin.Instance.UI.SendNotEnoughCurrencyModal(player.SteamID, ECurrency.COINS);

        _ = PendingWork.Remove(player.SteamID);
    }
}