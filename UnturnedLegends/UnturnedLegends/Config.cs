using Rocket.API;
using System.Collections.Generic;
using UnityEngine;
using UnturnedLegends.Models;

namespace UnturnedLegends
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

        public string KitName { get; set; }

        public bool EnableDebugLogs { get; set; }

        public Vector3 LobbySpawn { get; set; }

        public int BaseXP { get; set; }
        public double CommonRatio { get; set; }

        public int LastDamageAfterHealSeconds { get; set; }
        public float HealSeconds { get; set; }

        public int VoteSeconds { get; set; }
        public int GamesCount { get; set; }

        public int SpawnUnavailableSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public int EndingLeaderboardSeconds { get; set; }

        public int KillPoints { get; set; }
        public int DominationKills { get; set; }
        public int ShutdownKillStreak { get; set; }

        public FFAConfig FFA { get; set; }
        public TDMConfig TDM { get; set; }

        public List<ArenaLocation> ArenaLocations { get; set; }

        public void LoadDefaults()
        {
            DatabaseHost = "136.243.135.46";
            DatabaseUsername = "u476_0TqwYpW0Pe";
            DatabaseName = "s476_deathmatch";
            DatabasePassword = "k8gxtTbytcA5DXlqbn86e@+1";
            DatabasePort = "3306";
            PlayersTableName = "UL_Players";
            CacheRefreshSeconds = 600;

            KitName = "Starter";

            EnableDebugLogs = true;

            LobbySpawn = Vector3.zero;

            BaseXP = 200;
            CommonRatio = 1.20;

            LastDamageAfterHealSeconds = 5;
            HealSeconds = 0.065f;

            VoteSeconds = 60;
            GamesCount = 3;

            SpawnUnavailableSeconds = 5;
            RespawnSeconds = 5;

            EndingLeaderboardSeconds = 20;

            KillPoints = 100;
            DominationKills = 5;
            ShutdownKillStreak = 5;

            FFA = new FFAConfig(15, 600, 50, 50, 10, 5, 10, 5, 10, 15, 15, 15, 2);
            TDM = new TDMConfig(15, 600, 50, 50, 10, 5, 10, 5, 10, 15, 15, 15, 2);

            ArenaLocations = new List<ArenaLocation>
            {
                new ArenaLocation(1, 10, "Seattle", ""),
                new ArenaLocation(2, 10, "Tacoma", ""),
                new ArenaLocation(3, 10, "Military Base", "")
            };

        }
    }
}
