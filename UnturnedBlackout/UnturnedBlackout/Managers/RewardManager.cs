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
        _ = Task.Run(async () =>
        {
            Logging.Debug("Sending rewards");
            foreach (var reward in rewards)
            {
                switch (reward.RewardType)
                {
                    case ERewardType.GUN:
                        await db.AddPlayerGunAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.GUN_CHARM:
                        await db.AddPlayerGunCharmAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.GUN_SKIN:
                        await db.AddPlayerGunSkinAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.KNIFE:
                        await db.AddPlayerKnifeAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.GADGET:
                        await db.AddPlayerGadgetAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.KILLSTREAK:
                        await db.AddPlayerKillstreakAsync(steamID, Convert.ToInt32(reward.RewardValue), true);
                        break;
                    case ERewardType.PERK:
                        await db.AddPlayerPerkAsync(steamID, Convert.ToInt32(reward.RewardValue), true);
                        break;
                    case ERewardType.GLOVE:
                        await db.AddPlayerGloveAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.CARD:
                        await db.AddPlayerCardAsync(steamID, Convert.ToInt32(reward.RewardValue), true);
                        break;
                    case ERewardType.CREDIT:
                        await db.IncreasePlayerCreditsAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.COIN:
                        await db.IncreasePlayerCoinsAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.SCRAP:
                        await db.IncreasePlayerScrapAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.LEVEL_XP:
                        await db.IncreasePlayerXPAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.BATTLEPASS_XP:
                        await db.IncreasePlayerBPXPAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.CASE:
                        await db.IncreasePlayerCaseAsync(steamID, Convert.ToInt32(reward.RewardValue), 1);
                        break;
                    case ERewardType.BP_BOOSTER:
                        if (float.TryParse(reward.RewardValue.ToString(), out var booster))
                            await db.IncreasePlayerBoosterAsync(steamID, EBoosterType.BPXP, booster);
                        break;
                    case ERewardType.XP_BOOSTER:
                        if (float.TryParse(reward.RewardValue.ToString(), out booster))
                            await db.IncreasePlayerBoosterAsync(steamID, EBoosterType.XP, booster);
                        break;
                    case ERewardType.GUN_XP_BOOSTER:
                        if (float.TryParse(reward.RewardValue.ToString(), out booster))
                            await db.IncreasePlayerBoosterAsync(steamID, EBoosterType.GUNXP, booster);
                        break;
                }
            }
        });
    }

    public void RemoveRewards(CSteamID steamID, List<Reward> removeRewards)
    {
        var db = Plugin.Instance.DB;
        _ = Task.Run(async () =>
        {
            foreach (var reward in removeRewards)
            {
                switch (reward.RewardType)
                {
                    case ERewardType.GUN_CHARM:
                        await db.RemovePlayerGunCharmAsync(steamID, Convert.ToUInt16(reward.RewardValue));
                        break;
                    case ERewardType.CARD:
                        await db.RemovePlayerCardAsync(steamID, (int)reward.RewardValue);
                        break;
                    default:
                        break;
                }
            }
        });
    }

    public void GiveBulkRewards(List<(CSteamID, List<Reward>)> bulkRewards)
    {
        var db = Plugin.Instance.DB;
        _ = Task.Run(async () =>
        {
            foreach (var bulkReward in bulkRewards)
            {
                foreach (var reward in bulkReward.Item2)
                {
                    switch (reward.RewardType)
                    {
                        case ERewardType.GUN:
                            await db.AddPlayerGunAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.GUN_CHARM:
                            await db.AddPlayerGunCharmAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.GUN_SKIN:
                            await db.AddPlayerGunSkinAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.KNIFE:
                            await db.AddPlayerKnifeAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.GADGET:
                            await db.AddPlayerGadgetAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.KILLSTREAK:
                            await db.AddPlayerKillstreakAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue), true);
                            break;
                        case ERewardType.PERK:
                            await db.AddPlayerPerkAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue), true);
                            break;
                        case ERewardType.GLOVE:
                            await db.AddPlayerGloveAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.CARD:
                            await db.AddPlayerCardAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue), true);
                            break;
                        case ERewardType.CREDIT:
                            await db.IncreasePlayerCreditsAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.COIN:
                            await db.IncreasePlayerCoinsAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.SCRAP:
                            await db.IncreasePlayerScrapAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.LEVEL_XP:
                            await db.IncreasePlayerXPAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.BATTLEPASS_XP:
                            await db.IncreasePlayerBPXPAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.CASE:
                            await db.IncreasePlayerCaseAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue), 1);
                            break;
                        case ERewardType.BP_BOOSTER:
                            if (float.TryParse(reward.RewardValue.ToString(), out var booster))
                                await db.IncreasePlayerBoosterAsync(bulkReward.Item1, EBoosterType.BPXP, booster);
                            break;
                        case ERewardType.XP_BOOSTER:
                            if (float.TryParse(reward.RewardValue.ToString(), out booster))
                                await db.IncreasePlayerBoosterAsync(bulkReward.Item1, EBoosterType.XP, booster);
                            break;
                        case ERewardType.GUN_XP_BOOSTER:
                            if (float.TryParse(reward.RewardValue.ToString(), out booster))
                                await db.IncreasePlayerBoosterAsync(bulkReward.Item1, EBoosterType.GUNXP, booster);
                            break;
                    }
                }
            }
        });
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