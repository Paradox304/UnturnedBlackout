namespace UnturnedBlackout.Models.KC
{
    public class KCConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerMeleeKill { get; set; }
        public int XPPerKillHeadshot { get; set; }
        public int XPPerLethalKill { get; set; }
        public int XPPerAssist { get; set; }

        public int XPPerKillConfirmed { get; set; }
        public int XPPerKillDenied { get; set; }
        public int CollectorXP { get; set; }
        public int CollectorTags { get; set; }

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

        public ushort RedDogTagID { get; set; }
        public ushort BlueDogTagID { get; set; }

        public KCConfig()
        {

        }
        public KCConfig(int startSeconds, int endSeconds, int xPPerKill, int xPPerMeleeKill, int xPPerKillHeadshot, int xPPerLethalKill, int xPPerAssist, int xPPerKillConfirmed, int xPPerKillDenied, int collectorXP, int collectorTags, int baseXPMK, int increaseXPPerMK, int mKSeconds, int shutdownXP, int dominationXP, int revengeXP, int firstKillXP, int longshotXP, float winMultipler, int scoreLimit, int spawnProtectionSeconds, int respawnSeconds, ushort redDogTagID, ushort blueDogTagID)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            XPPerKill = xPPerKill;
            XPPerMeleeKill = xPPerMeleeKill;
            XPPerKillHeadshot = xPPerKillHeadshot;
            XPPerLethalKill = xPPerLethalKill;
            XPPerAssist = xPPerAssist;
            XPPerKillConfirmed = xPPerKillConfirmed;
            XPPerKillDenied = xPPerKillDenied;
            CollectorXP = collectorXP;
            CollectorTags = collectorTags;
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
            RedDogTagID = redDogTagID;
            BlueDogTagID = blueDogTagID;
        }
    }
}
