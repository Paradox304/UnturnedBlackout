using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Quest
    {
        public int QuestID { get; set; }
        public string QuestDesc { get; set; }
        public EQuestType QuestType { get; set; }
        public Dictionary<EQuestCondition, List<int>> QuestConditions { get; set; }
        public int TargetAmount { get; set; }
        public int XP { get; set; }

        public Quest(int questID, string questDesc, EQuestType questType, Dictionary<EQuestCondition, List<int>> questConditions, int targetAmount, int xP)
        {
            QuestID = questID;
            QuestDesc = questDesc;
            QuestType = questType;
            QuestConditions = questConditions;
            TargetAmount = targetAmount;
            XP = xP;
        }

    }
}
