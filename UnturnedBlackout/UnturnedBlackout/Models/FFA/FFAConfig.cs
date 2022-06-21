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

        public float WinMultipler { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public FFAConfig()
        {

        }

        public FFAConfig(int startSeconds, int endSeconds, Kit kit, List<TeamGlove> teamGloves, string killFeedHexCode, string chatPlayerHexCode, string chatMessageHexCode, int xPPerKill, int xPPerMeleeKill, int xPPerKillHeadshot, int xPPerLethalKill, int xPPerAssist, int baseXPMK, int increaseXPPerMK, int mKSeconds, int shutdownXP, int dominationXP, int revengeXP, float winMultipler, int scoreLimit, int spawnProtectionSeconds, int respawnSeconds)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Kit = kit;
            TeamGloves = teamGloves;
            KillFeedHexCode = killFeedHexCode;
            ChatPlayerHexCode = chatPlayerHexCode;
            ChatMessageHexCode = chatMessageHexCode;
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
            WinMultipler = winMultipler;
            ScoreLimit = scoreLimit;
            SpawnProtectionSeconds = spawnProtectionSeconds;
            RespawnSeconds = respawnSeconds;
        }
    }
}
