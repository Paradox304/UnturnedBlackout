namespace UnturnedBlackout.Models.CTF
{
    public class CTFConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerMeleeKill { get; set; }
        public int XPPerKillHeadshot { get; set; }
        public int XPPerLethalKill { get; set; }
        public int XPPerAssist { get; set; }

        public int XPPerFlagCaptured { get; set; }
        public int XPPerFlagSaved { get; set; }
        public int XPPerFlagKiller { get; set; }

        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }
        public int MKSeconds { get; set; }

        public int ShutdownXP { get; set; }
        public int DominationXP { get; set; }
        public int RevengeXP { get; set; }

        public float WinMultipler { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public ushort RedFlagID { get; set; }
        public ushort BlueFlagID { get; set; }

        public float FlagCarryingSpeed { get; set; }

        public CTFConfig()
        {

        }

        public CTFConfig(int startSeconds, int endSeconds, int xPPerKill, int xPPerMeleeKill, int xPPerKillHeadshot, int xPPerLethalKill, int xPPerAssist, int xPPerFlagCaptured, int xPPerFlagSaved, int xPPerFlagKiller, int baseXPMK, int increaseXPPerMK, int mKSeconds, int shutdownXP, int dominationXP, int revengeXP, float winMultipler, int scoreLimit, int spawnProtectionSeconds, int respawnSeconds, ushort redFlagID, ushort blueFlagID, float flagCarryingSpeed)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            XPPerKill = xPPerKill;
            XPPerMeleeKill = xPPerMeleeKill;
            XPPerKillHeadshot = xPPerKillHeadshot;
            XPPerLethalKill = xPPerLethalKill;
            XPPerAssist = xPPerAssist;
            XPPerFlagCaptured = xPPerFlagCaptured;
            XPPerFlagSaved = xPPerFlagSaved;
            XPPerFlagKiller = xPPerFlagKiller;
            BaseXPMK = baseXPMK;
            IncreaseXPPerMK = increaseXPPerMK;
            MKSeconds = mKSeconds;
            ShutdownXP = shutdownXP;
            DominationXP = dominationXP;
            RevengeXP = revengeXP;
            WinMultipler = winMultipler;
            ScoreLimit = scoreLimit;
            SpawnProtectionSeconds = spawnProtectionSeconds;
            RespawnSeconds = respawnSeconds;
            RedFlagID = redFlagID;
            BlueFlagID = blueFlagID;
            FlagCarryingSpeed = flagCarryingSpeed;
        }
    }
}
