using Steamworks;

namespace UnturnedBlackout.Database.Base
{
    public class LeaderboardData
    {
        public CSteamID SteamID { get; set; }
        public string SteamName { get; set; }
        public uint Level { get; set; }
        public uint Kills { get; set; }
        public uint HeadshotKills { get; set; }
        public uint Deaths { get; set; }

        public LeaderboardData(CSteamID steamID, string steamName, uint level, uint kills, uint headshotKills, uint deaths)
        {
            SteamID = steamID;
            SteamName = steamName;
            Level = level;
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
