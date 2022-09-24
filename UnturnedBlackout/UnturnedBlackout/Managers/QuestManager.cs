using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers
{
    public class QuestManager
    {
        public void CheckQuest(GamePlayer player, EQuestType questType, Dictionary<EQuestCondition, int> questConditions)
        {
            Steamworks.CSteamID steamID = player.Player.CSteamID;
            DatabaseManager db = Plugin.Instance.DB;
            PlayerData data = player.Data;

            Task.Run(() =>
            {
                Plugin.Instance.Achievement.CheckAchievement(player, questType, questConditions);
            });

            if (!data.QuestsSearchByType.TryGetValue(questType, out List<PlayerQuest> quests))
            {
                return;
            }

            List<PlayerQuest> pendingQuestsProgression = new();
            foreach (PlayerQuest quest in quests.Where(k => k.Amount < k.Quest.TargetAmount))
            {
                bool conditionsMet = true;
                foreach (KeyValuePair<EQuestCondition, List<int>> condition in quest.Quest.QuestConditions)
                {
                    if (!questConditions.TryGetValue(condition.Key, out int conditionValue))
                    {
                        conditionsMet = false;
                        break;
                    }

                    bool isConditionMinimum = IsConditionMinimum(condition.Key);
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

                quest.Amount += 1;
                if (quest.Amount >= quest.Quest.TargetAmount)
                {
                    Plugin.Instance.UI.SendAnimation(player, new Models.Animation.AnimationInfo(EAnimationType.QuestCompletion, quest.Quest));
                    Task.Run(async () =>
                    {
                        await db.IncreasePlayerBPXPAsync(steamID, quest.Quest.XP);
                    });
                }
                else if (quest.Amount * 100 / quest.Quest.TargetAmount % 10 == 0)
                {
                    pendingQuestsProgression.Add(quest);
                }

                Task.Run(async () =>
                {
                    await db.IncreasePlayerQuestAmountAsync(steamID, quest.Quest.QuestID, 1);
                });
            }

            if (pendingQuestsProgression.Count > 0)
            {
                Plugin.Instance.UI.SendQuestProgression(player, pendingQuestsProgression);
            }
        }

        public bool IsConditionMinimum(EQuestCondition condition)
        {
            int conditionInt = (int)condition;
            if (conditionInt >= 9 && conditionInt <= 12)
            {
                return true;
            }
            return false;
        }
    }
}
