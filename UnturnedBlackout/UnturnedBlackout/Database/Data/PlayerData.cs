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
        public int XP { get; set; }
        public int Level { get; set; }
        public int Credits { get; set; }
        public int Scrap { get; set; }
        public int Coins { get; set; }
        public int Kills { get; set; }
        public int HeadshotKills { get; set; }
        public int HighestKillstreak { get; set; }
        public int HighestMultiKills { get; set; }
        public int KillsConfirmed { get; set; }
        public int KillsDenied { get; set; }
        public int FlagsCaptured { get; set; }
        public int FlagsSaved { get; set; }
        public int AreasTaken { get; set; }
        public int Deaths { get; set; }
        public bool Music { get; set; }
        public bool IsMuted { get; set; }
        public DateTimeOffset MuteExpiry { get; set; }
        public List<PlayerQuest> Quests { get; set; }
        public Dictionary<EQuestType, List<PlayerQuest>> QuestsSearchByType { get; set; }
        public List<PlayerAchievement> Achievements { get; set; }
        public Dictionary<EQuestType, List<PlayerAchievement>> AchievementsSearchByType { get; set; }
        public Dictionary<int, PlayerAchievement> AchievementsSearchByID { get; set; }
        public PlayerBattlepass Battlepass { get; set; }

        public PlayerData(CSteamID steamID, string steamName, string avatarLink, int xP, int level, int credits, int scrap, int coins, int kills, int headshotKills, int highestKillstreak, int highestMultiKills, int killsConfirmed, int killsDenied, int flagsCaptured, int flagsSaved, int areasTaken, int deaths, bool music, bool isMuted, DateTimeOffset muteExpiry, List<PlayerQuest> quests, Dictionary<EQuestType, List<PlayerQuest>> questsSearchByType, List<PlayerAchievement> achievements, Dictionary<EQuestType, List<PlayerAchievement>> achievementsSearchByType, Dictionary<int, PlayerAchievement> achievementsSearchByID, PlayerBattlepass battlepass)
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
            Achievements = achievements;
            AchievementsSearchByType = achievementsSearchByType;
            AchievementsSearchByID = achievementsSearchByID;
            Battlepass = battlepass;
        }

        public bool TryGetNeededXP(out int xp)
        {
            if (Plugin.Instance.DBManager.Levels.TryGetValue(Level + 1, out XPLevel level))
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
                    await Plugin.Instance.DBManager.UpdatePlayerHighestMultiKillsAsync(SteamID, multiKills);
                });
            }
        }

        public void CheckKillstreak(int killStreak)
        {
            if (killStreak > HighestKillstreak)
            {
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DBManager.UpdatePlayerHighestKillStreakAsync(SteamID, killStreak);
                });
            }
        }
    }
}
