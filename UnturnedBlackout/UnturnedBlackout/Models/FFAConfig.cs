﻿namespace UnturnedBlackout.Models
{
    public class FFAConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerMeleeKill { get; set; }
        public int XPPerKillHeadshot { get; set; }
        public int XPPerAssist { get; set; }

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

        public FFAConfig(int startSeconds, int endSeconds, int xPPerKill, int xPPerMeleeKill, int xPPerKillHeadshot, int xPPerAssist, int baseXPKS, int increaseXPPerKS, int baseXPMK, int increaseXPPerMK, int mKSeconds, int shutdownXP, int dominationXP, int scoreLimit, int spawnProtectionSeconds)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            XPPerKill = xPPerKill;
            XPPerMeleeKill = xPPerMeleeKill;
            XPPerKillHeadshot = xPPerKillHeadshot;
            XPPerAssist = xPPerAssist;
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