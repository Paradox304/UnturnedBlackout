namespace UnturnedBlackout.Models.Configuration
{
    public class MedalsConfig
    {
        public int TurretDestroyXP { get; set; }
        public int NormalKillXP { get; set; }
        public int MeleeKillXP { get; set; }
        public int HeadshotKillXP { get; set; }
        public int KillstreakKillXP { get; set; }
        public int LethalKillXP { get; set; }
        public int LethalHitXP { get; set; }
        public int AssistKillXP { get; set; }
        public int ShutdownXP { get; set; }
        public int DominationXP { get; set; }
        public int RevengeXP { get; set; }
        public int FirstKillXP { get; set; }
        public int LongshotXP { get; set; }
        public int SurvivorXP { get; set; }
        public int FlagCapturedXP { get; set; }
        public int FlagSavedXP { get; set; }
        public int FlagCarrierKilledXP { get; set; }
        public int KillWhileCarryingFlagXP { get; set; }
        public int KillConfirmedXP { get; set; }
        public int KillDeniedXP { get; set; }
        public int CollectorXP { get; set; }
        public int CollectorTags { get; set; }
        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }
        public int MKSeconds { get; set; }
        public int DominationKills { get; set; }
        public int ShutdownKillStreak { get; set; }
        public int HealthSurvivorKill { get; set; }

        public MedalsConfig()
        {
            NormalKillXP = 0;
            MeleeKillXP = 0;
            HeadshotKillXP = 0;
            KillstreakKillXP = 0;
            LethalKillXP = 0;
            LethalHitXP = 0;
            AssistKillXP = 0;
            ShutdownXP = 0;
            DominationXP = 0;
            RevengeXP = 0;
            FirstKillXP = 0;
            LongshotXP = 0;
            SurvivorXP = 0;
            FlagCapturedXP = 0;
            FlagSavedXP = 0;
            FlagCarrierKilledXP = 0;
            KillWhileCarryingFlagXP = 0;
            KillConfirmedXP = 0;
            KillDeniedXP = 0;
            CollectorXP = 0;
            CollectorTags = 0;
            BaseXPMK = 0;
            IncreaseXPPerMK = 0;
            MKSeconds = 0;
            DominationKills = 0;
            ShutdownKillStreak = 0;
            HealthSurvivorKill = 0;
        }
    }
}
