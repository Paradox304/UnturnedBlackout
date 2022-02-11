using Rocket.API;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.Feed;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.KC;
using UnturnedBlackout.Models.Level;
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
        public string PlayersTableName { get; set; }
        public int CacheRefreshSeconds { get; set; }

        public string PlayerColorHexCode { get; set; }

        public int MaxKillFeed { get; set; }
        public int KillFeedFont { get; set; }
        public int KillFeedSeconds { get; set; }

        public int VoiceChatFont { get; set; }

        public bool EnableDebugLogs { get; set; }

        public Vector3 LobbySpawn { get; set; }

        public int LastDamageAfterHealSeconds { get; set; }
        public float HealSeconds { get; set; }
        public int HealAmount { get; set; }

        public int VoteSeconds { get; set; }
        public int GamesCount { get; set; }

        public int SpawnUnavailableSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public int EndingLeaderboardSeconds { get; set; }

        public int KillPoints { get; set; }
        public int AssistPoints { get; set; }
        public int KillConfirmedPoints { get; set; }
        public int KillDeniedPoints { get; set; }

        public int DominationKills { get; set; }
        public int ShutdownKillStreak { get; set; }

        public int SpawnSwitchThreshold { get; set; }
        public int SpawnSwitchCountSeconds { get; set; }
        public int SpawnSwitchTimeFrame { get; set; }
        public int SpawnSwitchSeconds { get; set; }

        public ushort KnifeID { get; set; }

        public FFAConfig FFA { get; set; }
        public TDMConfig TDM { get; set; }
        public KCConfig KC { get; set; }
        public CTFConfig CTF { get; set; }

        public List<ArenaLocation> ArenaLocations { get; set; }
        public List<LevelIcon> LevelIcons { get; set; }
        public List<FeedIcon> KillFeedIcons { get; set; }
        public List<LevelXP> LevelsXP { get; set; }
        public List<TeamInfo> TeamsInfo { get; set; }

        public List<ushort> AllowDamageBarricades { get; set; }

        public void LoadDefaults()
        {
            DatabaseHost = "136.243.135.46";
            DatabaseUsername = "u476_0TqwYpW0Pe";
            DatabaseName = "s476_deathmatch";
            DatabasePassword = "k8gxtTbytcA5DXlqbn86e@+1";
            DatabasePort = "3306";
            PlayersTableName = "UB_Players";
            CacheRefreshSeconds = 600;

            PlayerColorHexCode = "#FFFF00";
            MaxKillFeed = 5;
            KillFeedFont = 12;
            KillFeedSeconds = 5;

            VoiceChatFont = 30;

            EnableDebugLogs = true;

            LobbySpawn = Vector3.zero;

            LastDamageAfterHealSeconds = 5;
            HealSeconds = 0.065f;
            HealAmount = 10;

            VoteSeconds = 60;
            GamesCount = 3;

            SpawnUnavailableSeconds = 5;
            RespawnSeconds = 5;

            EndingLeaderboardSeconds = 20;

            KillPoints = 100;
            AssistPoints = 50;
            KillConfirmedPoints = 10;
            KillDeniedPoints = 10;

            DominationKills = 5;
            ShutdownKillStreak = 5;

            SpawnSwitchThreshold = 10;
            SpawnSwitchCountSeconds = 15;
            SpawnSwitchTimeFrame = 60;
            SpawnSwitchSeconds = 120;

            KnifeID = 58129;

            FFA = new FFAConfig(15, 600, "FFA", "white", "#dcb4ff", "#dcb4ff", 50, 60, 50, 20, 10, 5, 10, 15, 15, 1.5f, 15, 2);
            TDM = new TDMConfig(15, 600, 50, 60, 50, 20, 10, 5, 10, 15, 15, 1.5f, 15, 2);
            KC = new KCConfig(15, 600, 50, 60, 50, 20, 10, 10, 10, 5, 10, 15, 15, 1.5f, 15, 2, 26820, 26821);
            CTF = new CTFConfig(15, 600, 50, 60, 50, 20, 10, 10, 10, 5, 10, 15, 15, 1.5f, 3, 2, 26820, 26821);

            ArenaLocations = new List<ArenaLocation>
            {
                new ArenaLocation(1, 10, "Seattle", "", 0, 1, 2),
                new ArenaLocation(2, 10, "Tacoma", "", 1, 1, 2),
                new ArenaLocation(3, 10, "Military Base", "", 2, 1, 2)
            };
            LevelIcons = new List<LevelIcon>
            {
                new LevelIcon(0, "", "", ""),
                new LevelIcon(1, "", "", ""),
                new LevelIcon(2, "", "", "")
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
            LevelsXP = new List<LevelXP>
            {
                new LevelXP(2, 100),
                new LevelXP(3, 200)
            };
            TeamsInfo = new List<TeamInfo>
            {
                new TeamInfo(1, "Turned Ops", "#9ac5ff", "#9ac5ff", "#9ac5ff", "#9ac5ff", new List<string> { "TurnedOps" }),
                new TeamInfo(2, "Omega", "#ff7e7e", "#ff7e7e", "#ff7e7e", "#ff7e7e", new List<string> { "Omega" })
            };
            AllowDamageBarricades = new List<ushort> { 3, 4, 5 };
        }
    }
}
