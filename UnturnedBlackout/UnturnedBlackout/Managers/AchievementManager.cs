using Steamworks;
using System.Collections.Generic;
using System.Threading;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers
{
    public class AchievementManager
    {
        public void CheckAchievement(GamePlayer player, EQuestType achievementType, Dictionary<EQuestCondition, int> achievementConditions)
        {
            var steamID = player.Player.CSteamID;
            Logging.Debug($"Checking achievement {achievementType} for {steamID} with conditions {achievementConditions.Count}");

            var db = Plugin.Instance.DBManager;
            if (!db.PlayerData.TryGetValue(steamID, out PlayerData data))
            {
                Logging.Debug($"Error finding player data for player with steamID {steamID}");
                return;
            }

            if (!data.AchievementsSearchByType.TryGetValue(achievementType, out List<PlayerAchievement> achievements))
            {
                Logging.Debug($"Player has no achievement with type {achievementType}, returning");
                return;
            }

            Logging.Debug($"Found {achievements.Count} achievements with that type for player");
            foreach (var achievement in achievements)
            {
                var conditionsMet = true;
                Logging.Debug($"Checking achievement {achievement.Achievement.AchievementID}");
                foreach (var condition in achievement.Achievement.AchievementConditions)
                {
                    Logging.Debug($"Checking condition {condition.Key}");
                    if (!achievementConditions.TryGetValue(condition.Key, out int conditionValue))
                    {
                        Logging.Debug($"Condition {condition.Key} not found in achievementConditions, skipping");
                        conditionsMet = false;
                        break;
                    }

                    Logging.Debug($"Condition {condition.Key} found in achievementConditions, checking condition value {conditionValue}");

                    var isConditionMinimum = IsConditionMinimum(condition.Key);
                    if (!condition.Value.Contains(conditionValue) && !condition.Value.Exists(k => isConditionMinimum && conditionValue >= k) && !condition.Value.Contains(-1))
                    {
                        Logging.Debug($"Condition {condition.Key} value {conditionValue} not found in condition's value and -1 is also not found in condition's value, skipping");
                        conditionsMet = false;
                        break;
                    }
                }

                if (!conditionsMet)
                {
                    Logging.Debug($"Conditions not met for achievement {achievement.Achievement.AchievementID}, skipping");
                    continue;
                }

                Logging.Debug($"Conditions met for achievement {achievement.Achievement.AchievementID}, increasing achievement amount, current amount is {achievement.Amount}");
                achievement.Amount += 1;

                if (achievement.Achievement.TiersLookup.TryGetValue(achievement.CurrentTier + 1, out AchievementTier nextTier))
                {
                    if (achievement.Amount == nextTier.TargetAmount)
                    {
                        Plugin.Instance.UIManager.SendAnimation(player, new Models.Animation.AnimationInfo(EAnimationType.AchievementCompletion, nextTier));
                    }
                }

                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await db.IncreasePlayerAchievementAmountAsync(steamID, achievement.Achievement.AchievementID, 1);
                });
            }
        }

        public void ClaimAchievementTier(CSteamID steamID, int achievementID)
        {
            Logging.Debug($"Claiming achievement tier {achievementID} for {steamID}");

            var db = Plugin.Instance.DBManager;
            if (!db.PlayerData.TryGetValue(steamID, out PlayerData data))
            {
                Logging.Debug($"Error finding player data for player with steamID {steamID}");
                return;
            }

            if (!data.AchievementsSearchByID.TryGetValue(achievementID, out PlayerAchievement achievement))
            {
                Logging.Debug($"Player has no achievement with ID {achievementID}, returning");
                return;
            }

            Logging.Debug($"Found achievement {achievement.Achievement.AchievementID} with current tier {achievement.CurrentTier} and amount {achievement.Amount} for player");
            if (!achievement.Achievement.TiersLookup.TryGetValue(achievement.CurrentTier + 1, out AchievementTier nextTier))
            {
                Logging.Debug("Could'nt find next tier, returning");
                return;
            }

            Logging.Debug($"Found next tier {nextTier.TierID} with target amount {nextTier.TargetAmount}");
            if (achievement.Amount < nextTier.TargetAmount)
            {
                Logging.Debug($"Amount {achievement.Amount} is less than target amount {nextTier.TargetAmount}, returning");
                return;
            }

            Logging.Debug($"Amount {achievement.Amount} is greater than target amount {nextTier.TargetAmount}, increasing current tier to {nextTier.TierID}");
            achievement.CurrentTier = nextTier.TierID;
            Plugin.Instance.RewardManager.GiveReward(steamID, nextTier.Rewards);
            Plugin.Instance.RewardManager.RemoveReward(steamID, nextTier.RemoveRewards);
            Plugin.Instance.UIManager.OnAchievementsUpdated(steamID);

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DBManager.UpdatePlayerAchievementTierAsync(steamID, achievementID, nextTier.TierID);
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
