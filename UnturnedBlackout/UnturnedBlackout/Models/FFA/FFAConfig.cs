using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.FFA
{
    public class FFAConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public Kit Kit { get; set; }
        public List<TeamGlove> TeamGloves { get; set; }
        public string KillFeedHexCode { get; set; }
        public string ChatPlayerHexCode { get; set; }
        public string ChatMessageHexCode { get; set; }

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
        public int SurvivorXP { get; set; }

        public float WinMultipler { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public FFAConfig()
        {
            StartSeconds = 0;
            EndSeconds = 0;
            Kit = new Kit();
            TeamGloves = new List<TeamGlove> { new TeamGlove() };
            KillFeedHexCode = "";
            ChatPlayerHexCode = "";
            ChatMessageHexCode = "";
            XPPerKill = 0;
            XPPerMeleeKill = 0;
            XPPerKillHeadshot = 0;
            XPPerLethalKill = 0;
            XPPerAssist = 0;
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
        }
    }
}
