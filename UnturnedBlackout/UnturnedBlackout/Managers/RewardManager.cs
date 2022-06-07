using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Managers
{
    public class RewardManager
    {
        public RewardManager()
        {

        }

        public void GiveReward(CSteamID steamID, List<Reward> rewards)
        {
            var db = Plugin.Instance.DBManager;
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                foreach (var reward in rewards)
                {
                    var type = reward.RewardType;
                    switch (type)
                    {
                        case ERewardType.Gun:
                            await db.AddPlayerGunAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.GunCharm:
                            await db.AddPlayerGunCharmAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.GunSkin:
                            await db.AddPlayerGunSkinAsync(steamID, (int)reward.RewardValue);
                            break;
                        case ERewardType.Knife:
                            await db.AddPlayerKnifeAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.Gadget:
                            await db.AddPlayerGadgetAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.Killstreak:
                            await db.AddPlayerKillstreakAsync(steamID, (int)reward.RewardValue, true);
                            break;
                        case ERewardType.Perk:
                            await db.AddPlayerPerkAsync(steamID, (int)reward.RewardValue, true);
                            break;
                        case ERewardType.Glove:
                            await db.AddPlayerGloveAsync(steamID, Convert.ToUInt16(reward.RewardValue), true);
                            break;
                        case ERewardType.Card:
                            await db.AddPlayerCardAsync(steamID, (int)reward.RewardValue, true);
                            break;
                        case ERewardType.Credit:
                            await db.IncreasePlayerCreditsAsync(steamID, Convert.ToUInt32(reward.RewardValue));
                            break;
                        case ERewardType.Coin:
                            await db.IncreasePlayerCoinsAsync(steamID, Convert.ToUInt32(reward.RewardValue));
                            break;
                        case ERewardType.LevelXP:
                            await db.IncreasePlayerXPAsync(steamID, Convert.ToUInt32(reward.RewardValue));
                            break;
                        case ERewardType.BattlepassXP:
                            break;
                        case ERewardType.Crate:
                            break;
                    }
                }
            });
        }

        public void GiveBulkRewards(List<(CSteamID, List<Reward>)> bulkRewards)
        {
            var db = Plugin.Instance.DBManager;
            ThreadPool.QueueUserWorkItem(async (o) =>
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
                                await db.AddPlayerGunCharmAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                                break;
                            case ERewardType.GunSkin:
                                await db.AddPlayerGunSkinAsync(bulkReward.Item1, (int)reward.RewardValue);
                                break;
                            case ERewardType.Knife:
                                await db.AddPlayerKnifeAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                                break;
                            case ERewardType.Gadget:
                                await db.AddPlayerGadgetAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                                break;
                            case ERewardType.Killstreak:
                                await db.AddPlayerKillstreakAsync(bulkReward.Item1, (int)reward.RewardValue, true);
                                break;
                            case ERewardType.Perk:
                                await db.AddPlayerPerkAsync(bulkReward.Item1, (int)reward.RewardValue, true);
                                break;
                            case ERewardType.Glove:
                                await db.AddPlayerGloveAsync(bulkReward.Item1, Convert.ToUInt16(reward.RewardValue), true);
                                break;
                            case ERewardType.Card:
                                await db.AddPlayerCardAsync(bulkReward.Item1, (int)reward.RewardValue, true);
                                break;
                            case ERewardType.Credit:
                                await db.IncreasePlayerCreditsAsync(bulkReward.Item1, Convert.ToUInt32(reward.RewardValue));
                                break;
                            case ERewardType.Coin:
                                await db.IncreasePlayerCoinsAsync(bulkReward.Item1, Convert.ToUInt32(reward.RewardValue));
                                break;
                            case ERewardType.LevelXP:
                                await db.IncreasePlayerXPAsync(bulkReward.Item1, Convert.ToUInt32(reward.RewardValue));
                                break;
                            case ERewardType.BattlepassXP:
                                break;
                            case ERewardType.Crate:
                                break;
                        }
                    }
                }
            });
        }
    }
}
