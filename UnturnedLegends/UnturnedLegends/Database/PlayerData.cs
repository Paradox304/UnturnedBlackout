using Steamworks;
using System;

namespace UnturnedLegends.Database
{
    public class PlayerData
    {
        public CSteamID SteamID { get; set; }
        public string SteamName { get; set; }
        public string AvatarLink { get; set; }
        public uint XP { get; set; }
        public uint Level { get; set; }
        public uint Credits { get; set; }
        public uint Kills { get; set; }
        public uint HeadshotKills { get; set; }
        public uint HighestKillstreak { get; set; }
        public uint HighestMultiKills { get; set; }
        public uint KillsConfirmed { get; set; }
        public uint KillsDenied { get; set; }
        public uint FlagsCaptured { get; set; }
        public uint FlagsSaved { get; set; }
        public uint AreasTaken { get; set; }
        public uint Deaths { get; set; }

        public PlayerData(CSteamID steamID, string steamName, string avatarLink, uint xP, uint level, uint credits, uint kills, uint headshotKills, uint highestKillstreak, uint highestMultiKills, uint killsConfirmed, uint killsDenied, uint flagsCaptured, uint flagsSaved, uint areasTaken, uint deaths)
        {
            SteamID = steamID;
            SteamName = steamName;
            AvatarLink = avatarLink;
            XP = xP;
            Level = level;
            Credits = credits;
            Kills = kills;
            HeadshotKills = headshotKills;
            HighestKillstreak = highestKillstreak;
            HighestMultiKills = highestMultiKills;
            KillsConfirmed = killsConfirmed;
            KillsDenied = killsDenied;
            FlagsCaptured = flagsCaptured;
            FlagsSaved = flagsSaved;
            AreasTaken = areasTaken;
            Deaths = deaths;
        }

        public int GetNeededXP()
        {
            var config = Plugin.Instance.Configuration.Instance;
            return (int)(config.BaseXP * Math.Pow(config.CommonRatio, Level));
        }
    }
}
