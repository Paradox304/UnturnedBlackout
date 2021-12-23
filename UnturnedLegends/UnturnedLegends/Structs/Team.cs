﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedLegends.Structs
{
    public class Team
    {
        public string TeamName { get; set; }
        public HashSet<CSteamID> Players { get; set; }

        public CSteamID GroupID { get; set; }

        public Team(string teamName)
        {
            TeamName = teamName;
            Players = new HashSet<CSteamID>();
        }

        public void AddPlayer(CSteamID steamID)
        {
            Players.Add(steamID);
        }

        public void RemovePlayer(CSteamID steamID)
        {
            Players.Remove(steamID);
        }
    }
}