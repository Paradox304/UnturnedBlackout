﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class AchievementTier
    {
        public Achievement Achievement { get; set; }
        public int TierID { get; set; }
        public string TierTitle { get; set; }
        public string TierDesc { get; set; }
        public string TierPrevSmall { get; set; }
        public string TierPrevLarge { get; set; }
        public int TargetAmount { get; set; }
        public List<Reward> Rewards { get; set; }
        public List<Reward> RemoveRewards { get; set; }

        public AchievementTier(Achievement achievement, int tierID, string tierTitle, string tierDesc, string tierPrevSmall, string tierPrevLarge, int targetAmount, List<Reward> rewards, List<Reward> removeRewards)
        {
            Achievement = achievement;
            TierID = tierID;
            TierTitle = tierTitle;
            TierDesc = tierDesc;
            TierPrevSmall = tierPrevSmall;
            TierPrevLarge = tierPrevLarge;
            TargetAmount = targetAmount;
            Rewards = rewards;
            RemoveRewards = removeRewards;
        }
    }
}
