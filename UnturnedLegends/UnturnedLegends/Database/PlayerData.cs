using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedLegends.Database
{
    public class PlayerData
    {
        public CSteamID SteamID { get; set; }
        public string SteamName { get; set; }
        public string AvatarLink { get; set; }
        public uint XP { get; set; }
        public uint Credits { get; set; }
        public uint Kills { get; set; }
        public uint Deaths { get; set; }

        public PlayerData(CSteamID steamID, string steamName, string avatarLink, uint xP, uint credits, uint kills, uint deaths)
        {
            SteamID = steamID;
            SteamName = steamName;
            AvatarLink = avatarLink;
            XP = xP;
            Credits = credits;
            Kills = kills;
            Deaths = deaths;
        }
    }
}
