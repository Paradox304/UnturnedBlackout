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

            StartSeconds = 0;
            EndSeconds = 0;
            XPPerKill = 0;
            XPPerMeleeKill = 0;
            XPPerKillHeadshot = 0;
            XPPerLethalKill = 0;
            XPPerAssist = 0;
            XPPerKillConfirmed = 0;
            XPPerKillDenied = 0;
            CollectorXP = 0;
            CollectorTags = 0;
            BaseXPMK = 0;
            IncreaseXPPerMK = 0;
            MKSeconds = 0;
            ShutdownXP = 0;
            DominationXP = 0;
            RevengeXP = 0;
            FirstKillXP = 0;
            LongshotXP = 0;
            WinMultipler = 0;
            ScoreLimit = 0;
            SpawnProtectionSeconds = 0;
            RespawnSeconds = 0;
            RedDogTagID = 0;
            BlueDogTagID = 0;
        }
    }
}
