namespace UnturnedLegends.Models
{
    public class FFAConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerKillHeadshot { get; set; }

        public int BaseXPKS { get; set; }
        public int IncreaseXPPerKS { get; set; }

        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }
        public int MKSeconds { get; set; }

        public int ShutdownXP { get; set; }
        public int DominationXP { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }

        public FFAConfig()
        {

        }

        public FFAConfig(int startSeconds, int endSeconds, int xPPerKill, int xPPerKillHeadshot, int baseXPKS, int increaseXPPerKS, int baseXPMK, int increaseXPPerMK, int mKSeconds, int shutdownXP, int dominationXP, int scoreLimit, int spawnProtectionSeconds)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            XPPerKill = xPPerKill;
            XPPerKillHeadshot = xPPerKillHeadshot;
            BaseXPKS = baseXPKS;
            IncreaseXPPerKS = increaseXPPerKS;
            BaseXPMK = baseXPMK;
            IncreaseXPPerMK = increaseXPPerMK;
            MKSeconds = mKSeconds;
            ShutdownXP = shutdownXP;
            DominationXP = dominationXP;
            ScoreLimit = scoreLimit;
            SpawnProtectionSeconds = spawnProtectionSeconds;
        }
    }
}
