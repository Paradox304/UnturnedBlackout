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
        public int XPPerFlagDenied { get; set; }

        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }
        public int MKSeconds { get; set; }

        public int ShutdownXP { get; set; }
        public int DominationXP { get; set; }
        public int RevengeXP { get; set; }
        public int FirstKillXP { get; set; }
        public int LongshotXP { get; set; }
        public int SurvivorXP { get; set; }

        public float WinMultipler { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public ushort RedFlagID { get; set; }
        public ushort BlueFlagID { get; set; }

        public float FlagCarryingSpeed { get; set; }

        public CTFConfig()
        {
            StartSeconds = 0;
            EndSeconds = 0;
            XPPerKill = 0;
            XPPerMeleeKill = 0;
            XPPerKillHeadshot = 0;
            XPPerLethalKill = 0;
            XPPerAssist = 0;
            XPPerFlagCaptured = 0;
            XPPerFlagSaved = 0;
            XPPerFlagKiller = 0;
            XPPerFlagDenied = 0;
            BaseXPMK = 0;
            IncreaseXPPerMK = 0;
            MKSeconds = 0;
            ShutdownXP = 0;
            DominationXP = 0;
            RevengeXP = 0;
            FirstKillXP = 0;
            LongshotXP = 0;
            SurvivorXP = 0;
            WinMultipler = 0;
            ScoreLimit = 0;
            SpawnProtectionSeconds = 0;
            RespawnSeconds = 0;
            RedFlagID = 0;
            BlueFlagID = 0;
            FlagCarryingSpeed = 0;
        }
    }
}
