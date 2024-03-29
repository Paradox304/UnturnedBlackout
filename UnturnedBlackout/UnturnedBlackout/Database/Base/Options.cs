﻿using System;
using System.Collections.Generic;

namespace UnturnedBlackout.Database.Base;

public class Options
{
    public DateTimeOffset DailyLeaderboardWipe { get; set; }
    public DateTimeOffset WeeklyLeaderboardWipe { get; set; }
    public Dictionary<int, List<Reward>> DailyRankedRewards { get; set; }
    public List<PercentileReward> DailyPercentileRewards { get; set; }
    public Dictionary<int, List<Reward>> WeeklyRankedRewards { get; set; }
    public List<PercentileReward> WeeklyPercentileRewards { get; set; }
    public Dictionary<int, List<Reward>> SeasonalRankedRewards { get; set; }
    public List<PercentileReward> SeasonalPercentileRewards { get; set; }
    public float XPBooster { get; set; }
    public float BPBooster { get; set; }
    public float GunXPBooster { get; set; }
    public DateTimeOffset XPBoosterWipe { get; set; }
    public DateTimeOffset BPBoosterWipe { get; set; }
    public DateTimeOffset GunXPBoosterWipe { get; set; }
    public List<string> GameTips { get; set; }
    public List<Reward> PrimeRewards { get; set; }
    public List<Reward> PrimeDailyRewards { get; set; }
    public DateTimeOffset BattlepassExpiry { get; set; }

    public Options(DateTimeOffset dailyLeaderboardWipe, DateTimeOffset weeklyLeaderboardWipe, Dictionary<int, List<Reward>> dailyRankedRewards, List<PercentileReward> dailyPercentileRewards, Dictionary<int, List<Reward>> weeklyRankedRewards, List<PercentileReward> weeklyPercentileRewards, Dictionary<int, List<Reward>> seasonalRankedRewards, List<PercentileReward> seasonalPercentileRewards, float xpBooster, float bpBooster, float gunXPBooster, DateTimeOffset xpBoosterWipe, DateTimeOffset bpBoosterWipe, DateTimeOffset gunXPBoosterWipe, List<string> gameTips, List<Reward> primeRewards, List<Reward> primeDailyRewards, DateTimeOffset battlepassExpiry)
    {
        DailyLeaderboardWipe = dailyLeaderboardWipe;
        WeeklyLeaderboardWipe = weeklyLeaderboardWipe;
        DailyRankedRewards = dailyRankedRewards;
        DailyPercentileRewards = dailyPercentileRewards;
        WeeklyRankedRewards = weeklyRankedRewards;
        WeeklyPercentileRewards = weeklyPercentileRewards;
        SeasonalRankedRewards = seasonalRankedRewards;
        SeasonalPercentileRewards = seasonalPercentileRewards;
        XPBooster = xpBooster;
        BPBooster = bpBooster;
        GunXPBooster = gunXPBooster;
        XPBoosterWipe = xpBoosterWipe;
        BPBoosterWipe = bpBoosterWipe;
        GunXPBoosterWipe = gunXPBoosterWipe;
        GameTips = gameTips;
        PrimeRewards = primeRewards;
        PrimeDailyRewards = primeDailyRewards;
        BattlepassExpiry = battlepassExpiry;
    }
}