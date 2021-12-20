using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public string LocationTableName { get; set; }
        public int CacheRefreshSeconds { get; set; }


        public string MapName { get; set; }
        public string KitName { get; set; }

        public bool EnableDebugLogs { get; set; }

        public Vector3 LobbySpawn { get; set; }
        public List<ArenaLocation> ArenaLocations { get; set; }

        public void LoadDefaults()
        {
            DatabaseHost = "localhost";
            DatabaseUsername = "root";
            DatabaseName = "ParadoxCryptoCurrency";
            DatabasePassword = "root";
            DatabasePort = "3306";
            LocationTableName = "DB_Locations";
            CacheRefreshSeconds = 600;

            MapName = "Washington";
            KitName = "Starter";

            EnableDebugLogs = true;


            LobbySpawn = Vector3.zero;
            ArenaLocations = new List<ArenaLocation>
            {
                new ArenaLocation(1, "Seattle"),
                new ArenaLocation(2, "Tacoma"),
                new ArenaLocation(3, "Military Base")
            };
        }
    }
}
