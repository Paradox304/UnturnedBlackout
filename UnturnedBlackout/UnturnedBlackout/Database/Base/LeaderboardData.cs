using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class LeaderboardData
    {
        public CSteamID SteamID { get; set; }
        public uint Kills { get; set; }
        public uint HeadshotKills { get; set; }
        public uint Deaths { get; set; }

        public LeaderboardData(CSteamID steamID, uint kills, uint headshotKills, uint deaths)
        {
            SteamID = steamID;
            Kills = kills;
            HeadshotKills = headshotKills;
            Deaths = deaths;
        }

        public decimal GetKDR()
        {
            var kills = (decimal)(Kills + HeadshotKills);
            var deaths = (decimal)Deaths;
            decimal kdr = kills / deaths;
            return kdr;
        }
    }
}
