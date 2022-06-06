using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    }
}
