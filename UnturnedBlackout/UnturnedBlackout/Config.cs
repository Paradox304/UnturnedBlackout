using Rocket.API;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.Feed;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.KC;
using UnturnedBlackout.Models.TDM;

namespace UnturnedBlackout
{
    public class Config : IRocketPluginConfiguration
    {
        public string DatabaseHost { get; set; }
        public string DatabaseUsername { get; set; }
        public string DatabaseName { get; set; }
        public string DatabasePassword { get; set; }
        public string DatabasePort { get; set; }
        public int CacheRefreshSeconds { get; set; }

        public int MaxPlayerNameCharacters { get; set; }

        public string PlayerColorHexCode { get; set; }

        public string WebhookURL { get; set; }

        public int MaxKillFeed { get; set; }
        public int KillFeedFont { get; set; }
        public int KillFeedSeconds { get; set; }

        public int VoiceChatFont { get; set; }

        public bool UnlockAllItems { get; set; }
        public bool EnableDebugLogs { get; set; }

        public Vector3 LobbySpawn { get; set; }

        public int LastDamageAfterHealSeconds { get; set; }
        public float HealSeconds { get; set; }
        public int HealAmount { get; set; }

        public int GamesCount { get; set; }

        public float MovementStepsDelay { get; set; }

        public int SpawnUnavailableSeconds { get; set; }

        public int EndingLeaderboardSeconds { get; set; }

        public int KillPoints { get; set; }
        public int AssistPoints { get; set; }
        public int KillConfirmedPoints { get; set; }
        public int KillDeniedPoints { get; set; }
        public int FlagSavedPoints { get; set; }
        public int FlagCapturedPoints { get; set; }

        public int DominationKills { get; set; }
        public int ShutdownKillStreak { get; set; }

        public int SpawnSwitchThreshold { get; set; }
        public int SpawnSwitchCountSeconds { get; set; }
        public int SpawnSwitchTimeFrame { get; set; }
        public int SpawnSwitchSeconds { get; set; }

        public ushort KnifeID { get; set; }

        public int DefaultLoadoutAmount { get; set; }
        public List<LoadoutAmount> LoadoutAmounts { get; set; }
        public List<DefaultSkill> DefaultSkills { get; set; }

        public FFAConfig FFA { get; set; }
        public TDMConfig TDM { get; set; }
        public KCConfig KC { get; set; }
        public CTFConfig CTF { get; set; }

        public List<ArenaLocation> ArenaLocations { get; set; }
        public List<FeedIcon> KillFeedIcons { get; set; }
        public List<TeamInfo> TeamsInfo { get; set; }

        public void LoadDefaults()
        {
            DatabaseHost = "136.243.135.46";
            DatabaseUsername = "u476_0TqwYpW0Pe";
            DatabaseName = "s476_deathmatch";
            DatabasePassword = "k8gxtTbytcA5DXlqbn86e@+1";
            DatabasePort = "3306";
            CacheRefreshSeconds = 600;

            MaxPlayerNameCharacters = 30;

            PlayerColorHexCode = "#FFFF00";
            WebhookURL = "https://discord.com/api/webhooks/979000847197409280/e7Pbmjj_8bALCCDCbEDMCEVLX2ZSuIG3ymxbd-yb-IxkQ-sToxCkLJRmneeqB6LYVwgC";
            MaxKillFeed = 5;
            KillFeedFont = 12;
            KillFeedSeconds = 5;

            VoiceChatFont = 30;

            UnlockAllItems = false;
            EnableDebugLogs = true;

            LobbySpawn = Vector3.zero;

            LastDamageAfterHealSeconds = 5;
            HealSeconds = 0.065f;
            HealAmount = 10;

            GamesCount = 3;

            MovementStepsDelay = 0.2f;

            SpawnUnavailableSeconds = 5;

            EndingLeaderboardSeconds = 20;

            KillPoints = 100;
            AssistPoints = 50;
            KillConfirmedPoints = 10;
            KillDeniedPoints = 10;
            FlagSavedPoints = 50;
            FlagCapturedPoints = 50;

            DominationKills = 5;
            ShutdownKillStreak = 5;

            SpawnSwitchThreshold = 10;
            SpawnSwitchCountSeconds = 15;
            SpawnSwitchTimeFrame = 60;
            SpawnSwitchSeconds = 120;

            KnifeID = 58129;

            DefaultLoadoutAmount = 4;

            LoadoutAmounts = new List<LoadoutAmount>
            {
                new LoadoutAmount("VIP", 5),
                new LoadoutAmount("MVP", 6)
            };

            DefaultSkills = new List<DefaultSkill>
            {
                new DefaultSkill("OVERKILL", 1)
            };

            FFA = new FFAConfig(15, 600, new Kit(new List<ushort> { 173, 2, 1446 }), "white", "#dcb4ff", "#dcb4ff", 50, 60, 50, 50, 20, 10, 5, 10, 15, 15, 1.5f, 15, 2, 4);
            TDM = new TDMConfig(15, 600, 50, 60, 50, 50, 20, 10, 5, 10, 15, 15, 1.5f, 15, 2, 4);
            KC = new KCConfig(15, 600, 50, 60, 50, 50, 20, 10, 10, 10, 5, 10, 15, 15, 1.5f, 15, 2, 4, 26820, 26821);
            CTF = new CTFConfig(15, 600, 50, 60, 50, 50, 20, 10, 10, 10, 5, 10, 15, 15, 1.5f, 3, 2, 4, 26820, 26821, 0.75f);

            ArenaLocations = new List<ArenaLocation>
            {
                new ArenaLocation(1, "Seattle", "", 0, 0, 1, 0, 0, 0, 0, 2, 2, 2, 2)
            };

            KillFeedIcons = new List<FeedIcon>
            {
                new FeedIcon(28090, ""),
                new FeedIcon(28540, ""),
                new FeedIcon(28000, ""),
                new FeedIcon(28270, ""),
                new FeedIcon(28720, ""),
                new FeedIcon(28810, ""),
                new FeedIcon(28060, "")
            };

            TeamsInfo = new List<TeamInfo>
            {
                new TeamInfo(1, "Turned Ops", "#9ac5ff", "#9ac5ff", "#9ac5ff", "#9ac5ff", new List<Kit> { new Kit(new List<ushort> { 173, 2, 1446 }) }),
                new TeamInfo(2, "Omega", "#ff7e7e", "#ff7e7e", "#ff7e7e", "#ff7e7e", new List<Kit> { new Kit(new List<ushort> { 165, 2, 1446 }) })
            };
        }
    }
}
