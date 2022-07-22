namespace UnturnedBlackout.Database.Base
{
    public class BattlepassTier
    {
        public int TierID { get; set; }
        public Reward FreeReward { get; set; }
        public Reward PremiumReward { get; set; }
        public int XP { get; set; }

        public BattlepassTier(int tierID, Reward freeReward, Reward premiumReward, int xP)
        {
            TierID = tierID;
            FreeReward = freeReward;
            PremiumReward = premiumReward;
            XP = xP;
        }
    }
}
