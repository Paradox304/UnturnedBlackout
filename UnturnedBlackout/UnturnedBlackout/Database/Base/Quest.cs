using System.Collections.Generic;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base;

public class Quest
{
    public int QuestID { get; set; }
    public string QuestTitle { get; set; }
    public string QuestDesc { get; set; }
    public EQuestType QuestType { get; set; }
    public EQuestTier QuestTier { get; set; }
    public Dictionary<EQuestCondition, List<int>> QuestConditions { get; set; }
    public int TargetAmount { get; set; }
    public int XP { get; set; }

    public Quest(
        int questID,
        string questTitle,
        string questDesc,
        EQuestType questType,
        EQuestTier questTier,
        Dictionary<EQuestCondition, List<int>> questConditions,
        int targetAmount,
        int xP)
    {
        QuestID = questID;
        QuestTitle = questTitle;
        QuestDesc = questDesc;
        QuestType = questType;
        QuestTier = questTier;
        QuestConditions = questConditions;
        TargetAmount = targetAmount;
        XP = xP;
    }
}