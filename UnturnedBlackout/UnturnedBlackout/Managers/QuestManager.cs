using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rocket.Core.Utils;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers;

public class QuestManager
{
    public void CheckQuest(GamePlayer player, EQuestType questType, Dictionary<EQuestCondition, int> questConditions)
    {
        var steamID = player.Player.CSteamID;
        var db = Plugin.Instance.DB;
        var data = player.Data;

        TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Achievement.CheckAchievement(player, questType, questConditions));

        if (!data.QuestsSearchByType.TryGetValue(questType, out var quests))
            return;

        List<PlayerQuest> pendingQuestsProgression = new();
        foreach (var quest in quests.Where(k => k.Amount < k.Quest.TargetAmount))
        {
            var conditionsMet = true;
            foreach (var condition in quest.Quest.QuestConditions)
            {
                if (!questConditions.TryGetValue(condition.Key, out var conditionValue))
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
                continue;

            quest.Amount += 1;
            if (quest.Amount >= quest.Quest.TargetAmount)
            {
                Plugin.Instance.UI.SendAnimation(player, new(EAnimationType.QUEST_COMPLETION, quest.Quest));
                _ = Task.Run(async () => await db.IncreasePlayerBPXPAsync(steamID, quest.Quest.XP));
            }
            else if (quest.Amount * 100 / quest.Quest.TargetAmount % 10 == 0)
                pendingQuestsProgression.Add(quest);

            _ = Task.Run(async () => await db.IncreasePlayerQuestAmountAsync(steamID, quest.Quest.QuestID, 1));
        }

        if (pendingQuestsProgression.Count > 0)
            Plugin.Instance.UI.SendQuestProgression(player, pendingQuestsProgression);
    }

    private static bool IsConditionMinimum(EQuestCondition condition)
    {
        var conditionInt = (int)condition;
        return conditionInt is >= 9 and <= 12;
    }
}