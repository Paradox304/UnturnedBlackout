namespace UnturnedBlackout.Models.TDM
{
    public class TDMConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerMeleeKill { get; set; }
        public int XPPerKillHeadshot { get; set; }
        public int XPPerLethalKill { get; set; }
        public int XPPerAssist { get; set; }

        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }
        public int MKSeconds { get; set; }

        public int ShutdownXP { get; set; }
        public int DominationXP { get; set; }
        public int RevengeXP { get; set; }
        public int FirstKillXP { get; set; }
        public int LongshotXP { get; set; }

        public float WinMultipler { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public TDMConfig()
        {

        }

        public TDMConfig(int startSeconds, int endSeconds, int xPPerKill, int xPPerMeleeKill, int xPPerKillHeadshot, int xPPerLethalKill, int xPPerAssist, int baseXPMK, int increaseXPPerMK, int mKSeconds, int shutdownXP, int dominationXP, int revengeXP, int firstKillXP, int longshotXP, float winMultipler, int scoreLimit, int spawnProtectionSeconds, int respawnSeconds)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            XPPerKill = xPPerKill;
            XPPerMeleeKill = xPPerMeleeKill;
            XPPerKillHeadshot = xPPerKillHeadshot;
            XPPerLethalKill = xPPerLethalKill;
            XPPerAssist = xPPerAssist;
            BaseXPMK = baseXPMK;
            IncreaseXPPerMK = increaseXPPerMK;
            MKSeconds = mKSeconds;
            ShutdownXP = shutdownXP;
            DominationXP = dominationXP;
            RevengeXP = revengeXP;
            FirstKillXP = firstKillXP;
            LongshotXP = longshotXP;
            WinMultipler = winMultipler;
            ScoreLimit = scoreLimit;
            SpawnProtectionSeconds = spawnProtectionSeconds;
            RespawnSeconds = respawnSeconds;
        }
    }
}
