using Rocket.Core.Utils;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers
{
    public class AchievementManager
    {
        public void CheckAchievement(GamePlayer player, EQuestType achievementType, Dictionary<EQuestCondition, int> achievementConditions)
        {
            var steamID = player.Player.CSteamID;

            var db = Plugin.Instance.DB;
            var data = player.Data;

            if (!data.AchievementsSearchByType.TryGetValue(achievementType, out var achievements))
            {
                return;
            }

            foreach (var achievement in achievements)
            {
                var conditionsMet = true;
                foreach (var condition in achievement.Achievement.AchievementConditions)
                {
                    if (!achievementConditions.TryGetValue(condition.Key, out var conditionValue))
                    {
                        conditionsMet = false;
                        break;
                    }

                    var isConditionMinimum = IsConditionMinimum(condition.Key);
                    if (!condition.Value.Contains(conditionValue) && !condition.Value.Exists(k => isConditionMinimum && conditionValue >= k) && !condition.Value.Contains(-1))
                    {
                        conditionsMet = false;
                        break;
                    }
                }

                if (!conditionsMet)
                {
                    continue;
                }

                achievement.Amount += 1;

                if (achievement.Achievement.TiersLookup.TryGetValue(achievement.CurrentTier + 1, out var nextTier))
                {
                    if (achievement.Amount == nextTier.TargetAmount)
                    {
                        TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.SendAnimation(player, new Models.Animation.AnimationInfo(EAnimationType.AchievementCompletion, nextTier)));
                    }
                }

                Task.Run(async () =>
                {
                    await db.IncreasePlayerAchievementAmountAsync(steamID, achievement.Achievement.AchievementID, 1);
                });
            }
        }

        public void ClaimAchievementTier(CSteamID steamID, int achievementID)
        {
            var db = Plugin.Instance.DB;
            if (!db.PlayerData.TryGetValue(steamID, out var data))
            {
                Logging.Debug($"Error finding player data for player with steamID {steamID}");
                return;
            }

            if (!data.AchievementsSearchByID.TryGetValue(achievementID, out var achievement))
            {
                Logging.Debug($"Player has no achievement with ID {achievementID}, returning");
                return;
            }

            if (!achievement.Achievement.TiersLookup.TryGetValue(achievement.CurrentTier + 1, out var nextTier))
            {
                Logging.Debug("Could'nt find next tier, returning");
                return;
            }

            if (achievement.Amount < nextTier.TargetAmount)
            {
                Logging.Debug($"Amount {achievement.Amount} is less than target amount {nextTier.TargetAmount}, returning");
                return;
            }

            achievement.CurrentTier = nextTier.TierID;
            Plugin.Instance.Reward.GiveRewards(steamID, nextTier.Rewards);
            Plugin.Instance.Reward.RemoveRewards(steamID, nextTier.RemoveRewards);
            Plugin.Instance.UI.OnAchievementsUpdated(steamID);

            data.SetAchievementXPBooster();
            Task.Run(async () =>
            {
                await Plugin.Instance.DB.UpdatePlayerAchievementTierAsync(steamID, achievementID, nextTier.TierID);
            });
        }

        public bool IsConditionMinimum(EQuestCondition condition)
        {
            var conditionInt = (int)condition;
            if (conditionInt >= 9 && conditionInt <= 12)
            {
                return true;
            }
            return false;
        }
    }
}
