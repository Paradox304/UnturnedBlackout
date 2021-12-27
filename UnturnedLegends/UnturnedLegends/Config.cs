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

        public string MapName { get; set; }
        public string KitName { get; set; }

        public bool EnableDebugLogs { get; set; }

        public Vector3 LobbySpawn { get; set; }

        public int BaseXP { get; set; }
        public double CommonRatio { get; set; }

        public int LastDamageAfterHealSeconds { get; set; }
        public float HealSeconds { get; set; }

        public FFAConfig FFA { get; set; }

        public List<ArenaLocation> ArenaLocations { get; set; }
        public List<Arena> Arenas { get; set; }

        public void LoadDefaults()
        {
            DatabaseHost = "localhost";
            DatabaseUsername = "root";
            DatabaseName = "ParadoxCryptoCurrency";
            DatabasePassword = "root";
            DatabasePort = "3306";
            PlayersTableName = "UL_Players";
            CacheRefreshSeconds = 600;

            MapName = "Washington";
            KitName = "Starter";

            EnableDebugLogs = true;

            LobbySpawn = Vector3.zero;

            BaseXP = 200;
            CommonRatio = 1.20;

            LastDamageAfterHealSeconds = 5;
            HealSeconds = 0.075f;

            FFA = new FFAConfig(10, 600, 10, 30, 10, 5, 10, 10, 10, 5);

            ArenaLocations = new List<ArenaLocation>
            {
                new ArenaLocation(1, "Seattle"),
                new ArenaLocation(2, "Tacoma"),
                new ArenaLocation(3, "Military Base")
            };

            Arenas = new List<Arena>
            {
                new Arena(1, 6, new List<int> { 3 }),
                new Arena(2, 12, new List<int> { 2 }),
                new Arena(3, 24, new List<int> { 1 })
            };
        }
    }
}
