using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Managers
{
    public class QuestManager
    {
        public QuestManager()
        {

        }

        public void CheckQuest(CSteamID steamID, EQuestType questType, Dictionary<EQuestCondition, int> questConditions)
        {
            Logging.Debug($"Checking quest {questType} for {steamID} with conditions {questConditions.Count}");

            var db = Plugin.Instance.DBManager;
            if (!db.PlayerData.TryGetValue(steamID, out PlayerData data))
            {
                Logging.Debug($"Error finding player data for player with steamID {steamID}");
                return;
            }

            if (!data.QuestsSearchByType.TryGetValue(questType, out List<PlayerQuest> quests))
            {
                Logging.Debug($"Player has no quest with type {questType}, returning");
                return;
            }

            Logging.Debug($"Found {quests.Count} quests with that type for player");
            foreach (var quest in quests.Where(k => k.Amount < k.Quest.TargetAmount))
            {
                var conditionsMet = true;
                Logging.Debug($"Checking quest {quest.Quest.QuestID}");
                foreach (var condition in quest.Quest.QuestConditions)
                {
                    Logging.Debug($"Checking condition {condition.Key}");
                    if (!questConditions.TryGetValue(condition.Key, out int conditionValue))
                    {
                        Logging.Debug($"Condition {condition.Key} not found in questConditions, skipping");
                        conditionsMet = false;
                        break;
                    }

                    Logging.Debug($"Condition {condition.Key} found in questConditions, checking condition value {conditionValue}");

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
                    Logging.Debug($"Conditions not met for quest {quest.Quest.QuestID}, skipping");
                    continue;
                }

                Logging.Debug($"Conditions met for quest {quest.Quest.QuestID}, increasing quest amount, current amount is {quest.Amount}");
                quest.Amount += 1;
                if (quest.Amount >= quest.Quest.TargetAmount)
                {
                    Logging.Debug($"Quest with id {quest.Quest.QuestID} is completed for player");
                    // future code to increase the battlepass xp given by the quest here
                }

                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await db.IncreasePlayerQuestAmountAsync(steamID, quest.Quest.QuestID, 1);
                });
            }
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
