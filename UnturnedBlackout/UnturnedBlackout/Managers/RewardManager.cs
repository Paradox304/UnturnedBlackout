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
                    case ERewardType.Gun:
                        await db.AddPlayerGunAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.GunCharm:
                        await db.AddPlayerGunCharmAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.GunSkin:
                        await db.AddPlayerGunSkinAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.Knife:
                        await db.AddPlayerKnifeAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.Gadget:
                        await db.AddPlayerGadgetAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.Killstreak:
                        await db.AddPlayerKillstreakAsync(steamID, Convert.ToInt32(reward.RewardValue), true);
                        break;
                    case ERewardType.Perk:
                        await db.AddPlayerPerkAsync(steamID, Convert.ToInt32(reward.RewardValue), true);
                        break;
                    case ERewardType.Glove:
                        await db.AddPlayerGloveAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                        break;
                    case ERewardType.Card:
                        await db.AddPlayerCardAsync(steamID, Convert.ToInt32(reward.RewardValue), true);
                        break;
                    case ERewardType.Credit:
                        await db.IncreasePlayerCreditsAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.Coin:
                        await db.IncreasePlayerCoinsAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.Scrap:
                        await db.IncreasePlayerScrapAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.LevelXP:
                        await db.IncreasePlayerXPAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.BattlepassXP:
                        await db.IncreasePlayerBPXPAsync(steamID, Convert.ToInt32(reward.RewardValue));
                        break;
                    case ERewardType.Case:
                        await db.IncreasePlayerCaseAsync(steamID, Convert.ToInt32(reward.RewardValue), 1);
                        break;
                    case ERewardType.BPBooster:
                        if (float.TryParse(reward.RewardValue.ToString(), out var booster))
                            await db.IncreasePlayerBoosterAsync(steamID, EBoosterType.BPXP, booster);
                        break;
                    case ERewardType.XPBooster:
                        if (float.TryParse(reward.RewardValue.ToString(), out booster))
                            await db.IncreasePlayerBoosterAsync(steamID, EBoosterType.XP, booster);
                        break;
                    case ERewardType.GunXPBooster:
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
                    case ERewardType.GunCharm:
                        await db.RemovePlayerGunCharmAsync(steamID, Convert.ToUInt16(reward.RewardValue));
                        break;
                    case ERewardType.Card:
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
                        case ERewardType.Gun:
                            await db.AddPlayerGunAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.GunCharm:
                            await db.AddPlayerGunCharmAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue),
                                true);
                            break;
                        case ERewardType.GunSkin:
                            await db.AddPlayerGunSkinAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.Knife:
                            await db.AddPlayerKnifeAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.Gadget:
                            await db.AddPlayerGadgetAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.Killstreak:
                            await db.AddPlayerKillstreakAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue),
                                true);
                            break;
                        case ERewardType.Perk:
                            await db.AddPlayerPerkAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue), true);
                            break;
                        case ERewardType.Glove:
                            await db.AddPlayerGloveAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.Card:
                            await db.AddPlayerCardAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue), true);
                            break;
                        case ERewardType.Credit:
                            await db.IncreasePlayerCreditsAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.Coin:
                            await db.IncreasePlayerCoinsAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.Scrap:
                            await db.IncreasePlayerScrapAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.LevelXP:
                            await db.IncreasePlayerXPAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.BattlepassXP:
                            await db.IncreasePlayerBPXPAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue));
                            break;
                        case ERewardType.Case:
                            await db.IncreasePlayerCaseAsync(bulkReward.Item1, Convert.ToInt32(reward.RewardValue), 1);
                            break;
                        case ERewardType.BPBooster:
                            if (float.TryParse(reward.RewardValue.ToString(), out var booster))
                                await db.IncreasePlayerBoosterAsync(bulkReward.Item1, EBoosterType.BPXP, booster);
                            break;
                        case ERewardType.XPBooster:
                            if (float.TryParse(reward.RewardValue.ToString(), out booster))
                                await db.IncreasePlayerBoosterAsync(bulkReward.Item1, EBoosterType.XP, booster);
                            break;
                        case ERewardType.GunXPBooster:
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
            if (reward.RewardType != ERewardType.Coin && reward.RewardType != ERewardType.Credit) continue;

            if (reward.RewardValue is not int rewardValue) continue;

            Logging.Debug($"Reward value is {rewardValue}, reward type: {reward.RewardType}");
            reward.RewardValue = rewardValue * multiply;
            Logging.Debug($"Updated reward value is {rewardValue}");
        }
    }
}