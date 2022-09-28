using Steamworks;

namespace UnturnedBlackout.Database.Base;

public class LeaderboardData
{
    public CSteamID SteamID { get; set; }
    public string SteamName { get; set; }
    public string CountryCode { get; set; }
    public bool HideFlag { get; set; }
    public int Level { get; set; }
    public bool HasPrime { get; set; }
    public int Kills { get; set; }
    public int HeadshotKills { get; set; }
    public int Deaths { get; set; }

    public LeaderboardData(CSteamID steamID, string steamName, string countryCode, bool hideFlag, int level, bool hasPrime, int kills, int headshotKills, int deaths)
    {
        SteamID = steamID;
        SteamName = steamName;
        CountryCode = countryCode;
        HideFlag = hideFlag;
        Level = level;
        HasPrime = hasPrime;
        Kills = kills;
        HeadshotKills = headshotKills;
        Deaths = deaths;
    }

    public decimal GetKDR()
    {
        decimal kills = Kills + HeadshotKills;
        decimal deaths = Deaths;
        var kdr = kills / deaths;
        return kdr;
    }
}
