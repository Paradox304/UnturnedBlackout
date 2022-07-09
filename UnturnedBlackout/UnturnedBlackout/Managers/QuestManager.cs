using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers
{
    public class QuestManager
    {
        public void CheckQuest(GamePlayer player, EQuestType questType, Dictionary<EQuestCondition, int> questConditions)
        {
            var steamID = player.Player.CSteamID;
            var db = Plugin.Instance.DBManager;
            var data = player.Data;

            if (!data.QuestsSearchByType.TryGetValue(questType, out List<PlayerQuest> quests))
            {
                return;
            }

            var pendingQuestsProgression = new List<PlayerQuest>();
            foreach (var quest in quests.Where(k => k.Amount < k.Quest.TargetAmount))
            {
                var conditionsMet = true;
                foreach (var condition in quest.Quest.QuestConditions)
                {
                    if (!questConditions.TryGetValue(condition.Key, out int conditionValue))
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

                Logging.Debug($"Conditions met for quest {quest.Quest.QuestID}, increasing quest amount, current amount is {quest.Amount}, for {player.Player.CharacterName}");
                quest.Amount += 1;
                Logging.Debug($"Current quest amount: {quest.Amount}, target amount: {quest.Quest.TargetAmount}, remainder is: {quest.Amount * 100 / quest.Quest.TargetAmount % 100}");
                if (quest.Amount >= quest.Quest.TargetAmount)
                {
                    Logging.Debug($"Quest with id {quest.Quest.QuestID} is completed for player");
                    Plugin.Instance.UIManager.SendAnimation(player, new Models.Animation.AnimationInfo(EAnimationType.QuestCompletion, quest.Quest));
                    ThreadPool.QueueUserWorkItem(async (o) =>
                    {
                        await db.IncreasePlayerBPXPAsync(steamID, quest.Quest.XP);
                    });
                } else if (quest.Amount * 100 / quest.Quest.TargetAmount % 10 == 0)
                {
                    Logging.Debug("10% progression achieved, sending update UI");
                    pendingQuestsProgression.Add(quest);
                }

                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await db.IncreasePlayerQuestAmountAsync(steamID, quest.Quest.QuestID, 1);
                });
            }

            ThreadPool.QueueUserWorkItem((o) => Plugin.Instance.AchievementManager.CheckAchievement(player, questType, questConditions));
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
