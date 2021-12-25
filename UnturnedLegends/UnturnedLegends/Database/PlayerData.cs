using Steamworks;

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
        public uint Deaths { get; set; }

        public PlayerData(CSteamID steamID, string steamName, string avatarLink, uint xP, uint level, uint credits, uint kills, uint deaths)
        {
            SteamID = steamID;
            SteamName = steamName;
            AvatarLink = avatarLink;
            XP = xP;
            Level = level;
            Credits = credits;
            Kills = kills;
            Deaths = deaths;
        }
    }
}
