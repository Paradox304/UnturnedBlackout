﻿using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Data;

public class PlayerData
{
    public CSteamID SteamID { get; set; }
    public string SteamName { get; set; }
    public string AvatarLink { get; set; }
    public string CountryCode { get; set; }
    public bool HideFlag { get; set; }
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
    public bool HasBattlepass { get; set; }
    public float XPBooster { get; private set; }
    public float BPBooster { get; private set; }
    public float GunXPBooster { get; private set; }
    public float AchievementXPBooster { get; private set; }
    public bool HasPrime { get; set; }
    public DateTimeOffset PrimeExpiry { get; set; }
    public DateTimeOffset PrimeLastDailyReward { get; set; }

    public List<PlayerQuest> Quests { get; set; }
    public Dictionary<EQuestType, List<PlayerQuest>> QuestsSearchByType { get; set; }
    public List<PlayerAchievement> Achievements { get; set; }
    public Dictionary<EQuestType, List<PlayerAchievement>> AchievementsSearchByType { get; set; }
    public Dictionary<int, PlayerAchievement> AchievementsSearchByID { get; set; }
    public PlayerBattlepass Battlepass { get; set; }
    public List<PlayerBooster> ActiveBoosters { get; set; }
    public List<PlayerCase> Cases { get; set; }
    public Dictionary<int, PlayerCase> CasesSearchByID { get; set; }

    public PlayerData(CSteamID steamID, string steamName, string avatarLink, string countryCode, bool hideFlag, int xP, int level, int credits, int scrap, int coins, int kills, int headshotKills, int highestKillstreak, int highestMultiKills, int killsConfirmed, int killsDenied, int flagsCaptured, int flagsSaved, int areasTaken, int deaths, bool music, bool isMuted, DateTimeOffset muteExpiry, bool hasBattlepass, float xPBooster, float bPBooster, float gunXPBooster, bool hasPrime, DateTimeOffset primeExpiry, DateTimeOffset primeLastDailyReward)
    {
        SteamID = steamID;
        SteamName = steamName;
        AvatarLink = avatarLink;
        CountryCode = countryCode;
        HideFlag = hideFlag;
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
        HasBattlepass = hasBattlepass;
        XPBooster = xPBooster;
        BPBooster = bPBooster;
        GunXPBooster = gunXPBooster;
        HasPrime = hasPrime;
        PrimeExpiry = primeExpiry;
        PrimeLastDailyReward = primeLastDailyReward;
        Quests = new();
        QuestsSearchByType = new();
        Achievements = new();
        AchievementsSearchByType = new();
        AchievementsSearchByID = new();
        Battlepass = new();
        ActiveBoosters = new();
        Cases = new();
        CasesSearchByID = new();
    }

    public bool TryGetNeededXP(out int xp)
    {
        if (Plugin.Instance.DB.Levels.TryGetValue(Level + 1, out var level))
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
            Plugin.Instance.ActionDispatcher.QueueOnSecondThread(async () => await Plugin.Instance.DB.UpdatePlayerHighestMultiKillsAsync(SteamID, multiKills));
    }

    public void CheckKillstreak(int killStreak)
    {
        if (killStreak > HighestKillstreak)
            Plugin.Instance.ActionDispatcher.QueueOnSecondThread(async () => await Plugin.Instance.DB.UpdatePlayerHighestKillStreakAsync(SteamID, killStreak));
    }

    public void SetAchievementXPBooster()
    {
        var completedTiers = Achievements.Sum(k => k.CurrentTier);
        float totalTiers = Achievements.Sum(k => k.Achievement.Tiers.Max(tier => tier.TierID));

        var xpBooster = completedTiers / totalTiers;

        AchievementXPBooster = xpBooster;
    }

    public void SetPersonalBooster(EBoosterType type, float permanentBooster)
    {
        var boosters = ActiveBoosters.Where(k => k.BoosterType == type).ToList();
        var max = boosters.Count > 0 ? boosters.Max(k => k.BoosterValue) : 0f;
        var updatedValue = permanentBooster + max;
        switch (type)
        {
            case EBoosterType.XP:
                XPBooster = updatedValue;
                return;
            case EBoosterType.BPXP:
                BPBooster = updatedValue;
                return;
            case EBoosterType.GUNXP:
                GunXPBooster = updatedValue;
                return;
        }
    }

    public int GetCurrency(ECurrency currency) => currency switch
    {
        ECurrency.COINS => Coins,
        ECurrency.SCRAP => Scrap,
        ECurrency.CREDITS => Credits,
        var _ => throw new ArgumentOutOfRangeException("currency", "Currency is not as expected")
    };
}