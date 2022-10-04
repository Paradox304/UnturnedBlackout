using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Managers;

public class RewardManager
{
    public void GiveRewards(CSteamID steamID, List<Reward> rewards)
    {
        Logging.Debug($"Giving rewards to {steamID}, rewards: {rewards.Count}");
        var db = Plugin.Instance.DB;
        foreach (var reward in rewards)
        {
            switch (reward.RewardType)
            {
                case ERewardType.GUN:
                    db.AddPlayerGunBought(steamID, Convert.ToUInt16(reward.RewardValue));
                    break;
                case ERewardType.GUN_CHARM:
                    db.AddPlayerGunCharmBought(steamID, Convert.ToUInt16(reward.RewardValue));
                    break;
                case ERewardType.GUN_SKIN:
                    db.AddPlayerGunSkin(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.KNIFE:
                    db.AddPlayerKnifeBought(steamID, Convert.ToUInt16(reward.RewardValue));
                    break;
                case ERewardType.GADGET:
                    db.AddPlayerGadgetBought(steamID, Convert.ToUInt16(reward.RewardValue));
                    break;
                case ERewardType.KILLSTREAK:
                    db.AddPlayerKillstreakBought(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.PERK:
                    db.AddPlayerPerkBought(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.GLOVE:
                    db.AddPlayerGloveBought(steamID, Convert.ToUInt16(reward.RewardValue));
                    break;
                case ERewardType.CARD:
                    db.AddPlayerCardBought(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.CREDIT:
                    db.IncreasePlayerCredits(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.COIN:
                    db.IncreasePlayerCoins(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.SCRAP:
                    db.IncreasePlayerScrap(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.LEVEL_XP:
                    db.IncreasePlayerXP(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.BATTLEPASS_XP:
                    db.IncreasePlayerBPXP(steamID, Convert.ToInt32(reward.RewardValue));
                    break;
                case ERewardType.CASE:
                    db.IncreasePlayerCase(steamID, Convert.ToInt32(reward.RewardValue), 1);
                    break;
                case ERewardType.BP_BOOSTER:
                {
                    if (float.TryParse(reward.RewardValue.ToString(), out var booster))
                        db.IncreasePlayerBooster(steamID, EBoosterType.BPXP, booster);

                    break;
                }
                case ERewardType.XP_BOOSTER:
                {
                    if (float.TryParse(reward.RewardValue.ToString(), out var booster))
                        db.IncreasePlayerBooster(steamID, EBoosterType.XP, booster);

                    break;
                }
                case ERewardType.GUN_XP_BOOSTER:
                {
                    if (float.TryParse(reward.RewardValue.ToString(), out var booster))
                        db.IncreasePlayerBooster(steamID, EBoosterType.GUNXP, booster);

                    break;
                }
            }
        }
    }

    public void RemoveRewards(CSteamID steamID, List<Reward> removeRewards)
    {
        var db = Plugin.Instance.DB;
        foreach (var reward in removeRewards)
        {
            switch (reward.RewardType)
            {
                case ERewardType.GUN_CHARM:
                    db.RemovePlayerGunCharm(steamID, Convert.ToUInt16(reward.RewardValue));
                    break;
                case ERewardType.CARD:
                    db.RemovePlayerCard(steamID, (int)reward.RewardValue);
                    break;
            }
        }
    }

    public void GiveBulkRewards(List<(CSteamID, List<Reward>)> bulkRewards)
    {
        var db = Plugin.Instance.DB;
        foreach (var bulkReward in bulkRewards)
        {
            foreach (var reward in bulkReward.Item2)
            {
                switch (reward.RewardType)
                {
                    case ERewardType.GUN:
                        db.AddPlayerGunBought(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue));
                        break;
                    case ERewardType.GUN_CHARM:
                        db.AddPlayerGunCharmBought(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue));
                        break;
                    case ERewardType.GUN_SKIN:
                        db.AddPlayerGunSkin(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.KNIFE:
                        db.AddPlayerKnifeBought(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue));
                        break;
                    case ERewardType.GADGET:
                        db.AddPlayerGadgetBought(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue));
                        break;
                    case ERewardType.KILLSTREAK:
                        db.AddPlayerKillstreakBought(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.PERK:
                        db.AddPlayerPerkBought(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.GLOVE:
                        db.AddPlayerGloveBought(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue));
                        break;
                    case ERewardType.CARD:
                        db.AddPlayerCardBought(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.CREDIT:
                        db.IncreasePlayerCredits(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.COIN:
                        db.IncreasePlayerCoins(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.SCRAP:
                        db.IncreasePlayerScrap(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.LEVEL_XP:
                        db.IncreasePlayerXP(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.BATTLEPASS_XP:
                        db.IncreasePlayerBPXP(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.CASE:
                        db.IncreasePlayerCase(bulkReward.Item1, Convert.ToInt32(reward.RewardValue), 1);
                        break;
                    case ERewardType.BP_BOOSTER:
                        if (float.TryParse(reward.RewardValue.ToString(), out var booster))
                            db.IncreasePlayerBooster(bulkReward.Item1, EBoosterType.BPXP, booster);

                        break;
                    case ERewardType.XP_BOOSTER:
                        if (float.TryParse(reward.RewardValue.ToString(), out booster))
                            db.IncreasePlayerBooster(bulkReward.Item1, EBoosterType.XP, booster);

                        break;
                    case ERewardType.GUN_XP_BOOSTER:
                        if (float.TryParse(reward.RewardValue.ToString(), out booster))
                            db.IncreasePlayerBooster(bulkReward.Item1, EBoosterType.GUNXP, booster);

                        break;
                }
            }
        }
    }

    public void MultiplyRewards(List<Reward> rewards, int multiply)
    {
        Logging.Debug($"Multiplying rewards by {multiply}");
        foreach (var reward in rewards)
        {
            if (reward.RewardType != ERewardType.COIN && reward.RewardType != ERewardType.CREDIT)
                continue;

            if (reward.RewardValue is not int rewardValue)
                continue;

            Logging.Debug($"Reward value is {rewardValue}, reward type: {reward.RewardType}");
            reward.RewardValue = rewardValue * multiply;
            Logging.Debug($"Updated reward value is {rewardValue}");
        }
    }
}