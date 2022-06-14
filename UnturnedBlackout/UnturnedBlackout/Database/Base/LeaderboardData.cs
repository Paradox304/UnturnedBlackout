using Steamworks;

namespace UnturnedBlackout.Database.Base
{
    public class LeaderboardData
    {
        public CSteamID SteamID { get; set; }
        public string SteamName { get; set; }
        public int Level { get; set; }
        public int Kills { get; set; }
        public int HeadshotKills { get; set; }
        public int Deaths { get; set; }

        public LeaderboardData(CSteamID steamID, string steamName, int level, int kills, int headshotKills, int deaths)
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
