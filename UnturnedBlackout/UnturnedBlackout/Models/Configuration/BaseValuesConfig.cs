using UnityEngine;

namespace UnturnedBlackout.Models.Configuration
{
    public class BaseValuesConfig
    {
        public int MaxPlayerNameCharacters { get; set; }
        public string PlayerColorHexCode { get; set; }
        public string HardcoreColor { get; set; }
        public string WebhookURL { get; set; }
        public int MaxKillFeed { get; set; }
        public int KillFeedFont { get; set; }
        public int KillFeedSeconds { get; set; }
        public int VoiceChatFont { get; set; }
        public bool EnableDebugLogs { get; set; }
        public Vector3 LobbySpawn { get; set; }
        public float LobbyYaw { get; set; }
        public int LastDamageAfterHealSeconds { get; set; }
        public float HealSeconds { get; set; }
        public float HealAmount { get; set; }
        public int GamesCount { get; set; }
        public float MovementStepsDelay { get; set; }
        public int SpawnUnavailableSeconds { get; set; }
        public int EndingLeaderboardSeconds { get; set; }
        public int SendingTipSeconds { get; set; }

        public int SpawnSwitchThreshold { get; set; }
        public int SpawnSwitchCountSeconds { get; set; }
        public int SpawnSwitchTimeFrame { get; set; }
        public int SpawnSwitchSeconds { get; set; }

        public BaseValuesConfig()
        {
            MaxPlayerNameCharacters = 20;

            PlayerColorHexCode = "#FFFF00";
            HardcoreColor = "red";
            WebhookURL = "https://discord.com/api/webhooks/979000847197409280/e7Pbmjj_8bALCCDCbEDMCEVLX2ZSuIG3ymxbd-yb-IxkQ-sToxCkLJRmneeqB6LYVwgC";
            MaxKillFeed = 6;
            KillFeedFont = 40;
            KillFeedSeconds = 10;

            VoiceChatFont = 30;

            EnableDebugLogs = true;

            LobbySpawn = new Vector3(353.027039f, 54.5521927f, -3792.77026f);
            LobbyYaw = 100f;

            LastDamageAfterHealSeconds = 3;
            HealSeconds = 0.5f;
            HealAmount = 10;

            GamesCount = 9;

            MovementStepsDelay = 0;

            SpawnUnavailableSeconds = 5;

            EndingLeaderboardSeconds = 15;

            SpawnSwitchThreshold = 5;
            SpawnSwitchCountSeconds = 11;
            SpawnSwitchTimeFrame = 27;
            SpawnSwitchSeconds = 120;
        }
    }
}
