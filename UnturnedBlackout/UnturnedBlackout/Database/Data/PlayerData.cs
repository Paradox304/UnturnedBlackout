using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Data
{
    public class PlayerData
    {
        public CSteamID SteamID { get; set; }
        public string SteamName { get; set; }
        public string AvatarLink { get; set; }
        public uint XP { get; set; }
        public uint Level { get; set; }
        public uint Credits { get; set; }
        public uint Scrap { get; set; }
        public uint Coins { get; set; }
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
        public bool Music { get; set; }
        public bool IsMuted { get; set; }
        public DateTimeOffset MuteExpiry { get; set; }
        public List<PlayerQuest> Quests { get; set; }
        public Dictionary<EQuestType, List<PlayerQuest>> QuestsSearchByType { get; set; }

        public PlayerData(CSteamID steamID, string steamName, string avatarLink, uint xP, uint level, uint credits, uint scrap, uint coins, uint kills, uint headshotKills, uint highestKillstreak, uint highestMultiKills, uint killsConfirmed, uint killsDenied, uint flagsCaptured, uint flagsSaved, uint areasTaken, uint deaths, bool music, bool isMuted, DateTimeOffset muteExpiry, List<PlayerQuest> quests, Dictionary<EQuestType, List<PlayerQuest>> questsSearchByType)
        {
            SteamID = steamID;
            SteamName = steamName;
            AvatarLink = avatarLink;
            XP = xP;
            Level = level;
            Credits = credits;
            Scrap = scrap;
            Coins = coins;
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
            Music = music;
            IsMuted = isMuted;
            MuteExpiry = muteExpiry;
            Quests = quests;
            QuestsSearchByType = questsSearchByType;
        }

        public bool TryGetNeededXP(out int xp)
        {
            if (Plugin.Instance.DBManager.Levels.TryGetValue((int)Level + 1, out XPLevel level))
            {
                xp = level.XPNeeded;
                return true;
            }
            xp = 0;
            return false;
        }

        public void CheckMultipleKills(int multiKills)
        {
            if (multiKills > HighestMultiKills)
            {
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DBManager.UpdatePlayerHighestMultiKillsAsync(SteamID, (uint)multiKills);
                });
            }
        }

        public void CheckKillstreak(int killStreak)
        {
            if (killStreak > HighestKillstreak)
            {
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DBManager.UpdatePlayerHighestKillStreakAsync(SteamID, (uint)killStreak);
                });
            }
        }
    }
}
