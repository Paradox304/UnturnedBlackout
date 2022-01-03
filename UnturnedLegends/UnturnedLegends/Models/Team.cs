using Steamworks;
using System.Collections.Generic;

namespace UnturnedLegends.Models
{
    public class Team
    {
        public int TeamID { get; set; }
        public HashSet<CSteamID> Players { get; set; }

        public CSteamID GroupID { get; set; }

        public Team(int teamID)
        {
            TeamID = teamID;
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
