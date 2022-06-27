namespace UnturnedBlackout.Database.Base
{
    public class BattlepassTier
    {
        public int TierID { get; set; }
        public Reward FreeReward { get; set; }
        public string FreeRewardIcon { get; set; }
        public Reward PremiumReward { get; set; }
        public string PremiumRewardIcon { get; set; }
        public int XP { get; set; }

        public BattlepassTier(int tierID, Reward freeReward, string freeRewardIcon, Reward premiumReward, string premiumRewardIcon, int xP)
        {
            TierID = tierID;
            FreeReward = freeReward;
            FreeRewardIcon = freeRewardIcon;
            PremiumReward = premiumReward;
            PremiumRewardIcon = premiumRewardIcon;
            XP = xP;
        }
    }
}
