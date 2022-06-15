using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Achievement
    {
        public int AchievementID { get; set; }
        public EQuestType AchievementType { get; set; }
        public Dictionary<EQuestCondition, List<int>> AchievementConditions { get; set; }
        public List<AchievementTier> Tiers { get; set; }
        public Dictionary<int, AchievementTier> TiersLookup { get; set; }

        public Achievement(int achievementID, EQuestType achievementType, Dictionary<EQuestCondition, List<int>> achievementConditions, List<AchievementTier> tiers, Dictionary<int, AchievementTier> tiersLookup)
        {
            AchievementID = achievementID;
            AchievementType = achievementType;
            AchievementConditions = achievementConditions;
            Tiers = tiers;
            TiersLookup = tiersLookup;
        }
    }
}
