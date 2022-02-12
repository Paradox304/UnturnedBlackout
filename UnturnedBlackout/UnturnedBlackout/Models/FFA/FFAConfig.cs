namespace UnturnedBlackout.Models.FFA
{
    public class FFAConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public string KitName { get; set; }
        public string KillFeedHexCode { get; set; }
        public string ChatPlayerHexCode { get; set; }
        public string ChatMessageHexCode { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerMeleeKill { get; set; }
        public int XPPerKillHeadshot { get; set; }
        public int XPPerAssist { get; set; }

        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }
        public int MKSeconds { get; set; }

        public int ShutdownXP { get; set; }
        public int DominationXP { get; set; }

        public float WinMultipler { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public FFAConfig()
        {

        }

        public FFAConfig(int startSeconds, int endSeconds, string kitName, string killFeedHexCode, string chatPlayerHexCode, string chatMessageHexCode, int xPPerKill, int xPPerMeleeKill, int xPPerKillHeadshot, int xPPerAssist, int baseXPMK, int increaseXPPerMK, int mKSeconds, int shutdownXP, int dominationXP, float winMultipler, int scoreLimit, int spawnProtectionSeconds, int respawnSeconds)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            KitName = kitName;
            KillFeedHexCode = killFeedHexCode;
            ChatPlayerHexCode = chatPlayerHexCode;
            ChatMessageHexCode = chatMessageHexCode;
            XPPerKill = xPPerKill;
            XPPerMeleeKill = xPPerMeleeKill;
            XPPerKillHeadshot = xPPerKillHeadshot;
            XPPerAssist = xPPerAssist;
            BaseXPMK = baseXPMK;
            IncreaseXPPerMK = increaseXPPerMK;
            MKSeconds = mKSeconds;
            ShutdownXP = shutdownXP;
            DominationXP = dominationXP;
            WinMultipler = winMultipler;
            ScoreLimit = scoreLimit;
            SpawnProtectionSeconds = spawnProtectionSeconds;
            RespawnSeconds = respawnSeconds;
        }
    }
}
