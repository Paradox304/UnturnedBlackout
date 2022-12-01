using System.Linq;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers;

public class BPManager
{
    public DatabaseManager DB => Plugin.Instance.DB;

    public ConfigManager Config => Plugin.Instance.Config;

    public bool ClaimReward(GamePlayer player, bool isTop, int tierID)
    {
        if (!DB.BattlepassTiersSearchByID.TryGetValue(tierID, out var tier))
        {
            Logging.Debug($"Error finding battlepass tier with id {tierID} for {player.Player.CharacterName}");
            return false;
        }

        var bp = player.Data.Battlepass;
        if ((isTop && bp.ClaimedFreeRewards.Contains(tierID)) || (!isTop && bp.ClaimedPremiumRewards.Contains(tierID)))
        {
            Logging.Debug($"{player.Player.CharacterName} has already claimed the reward for IsTop {isTop} and selected {tierID}, why is the plugin asking again?");
            return false;
        }

        Reward reward;
        if (isTop)
        {
            reward = tier.FreeReward;
            _ = bp.ClaimedFreeRewards.Add(tierID);
        }
        else
        {
            if (!player.Data.HasBattlepass)
            {
                Logging.Debug($"{player.Player.CharacterName} is trying to claim a premium reward for battlepass while not having it in the first place");
                return false;
            }
            
            reward = tier.PremiumReward;
            _ = bp.ClaimedPremiumRewards.Add(tierID);
        }

        Plugin.Instance.Reward.GiveRewards(player.SteamID, new() { reward });

        if (isTop)
            DB.UpdatePlayerBPClaimedFreeRewards(player.SteamID);
        else
            DB.UpdatePlayerBPClaimedPremiumRewards(player.SteamID);

        return true;
    }

    public bool SkipTier(GamePlayer player)
    {
        var bp = player.Data.Battlepass;
        if (!DB.BattlepassTiersSearchByID.ContainsKey(bp.CurrentTier + 1))
        {
            Logging.Debug($"{player.Player.CharacterName} has already reached the end of battlepass");
            return false;
        }

        if (bp.CurrentTier == DB.BattlepassTiersSearchByID.Keys.Max())
        {
            Logging.Debug($"{player.Player.CharacterName} is already on the max tier");
            return false;
        }
        
        if (player.Data.Coins >= Config.Base.FileData.BattlepassTierSkipCost)
        {
            bp.CurrentTier += 1;
            
            DB.DecreasePlayerCoins(player.SteamID, Config.Base.FileData.BattlepassTierSkipCost);
            DB.UpdatePlayerBPTier(player.SteamID, bp.CurrentTier);
            return true;
        }

        Plugin.Instance.UI.SendNotEnoughCurrencyModal(player.SteamID, ECurrency.COIN);
        return false;
    }
}