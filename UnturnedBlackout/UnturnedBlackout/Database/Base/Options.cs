﻿using System;
using System.Collections.Generic;

namespace UnturnedBlackout.Database.Base
{
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
        public DateTimeOffset XPBoosterWipe { get; set; }
        public DateTimeOffset BPBoosterWipe { get; set; }

        public Options(DateTimeOffset dailyLeaderboardWipe, DateTimeOffset weeklyLeaderboardWipe, Dictionary<int, List<Reward>> dailyRankedRewards, List<PercentileReward> dailyPercentileRewards, Dictionary<int, List<Reward>> weeklyRankedRewards, List<PercentileReward> weeklyPercentileRewards, Dictionary<int, List<Reward>> seasonalRankedRewards, List<PercentileReward> seasonalPercentileRewards, float xPBooster, float bPBooster, DateTimeOffset xPBoosterWipe, DateTimeOffset bPBoosterWipe)
        {
            DailyLeaderboardWipe = dailyLeaderboardWipe;
            WeeklyLeaderboardWipe = weeklyLeaderboardWipe;
            DailyRankedRewards = dailyRankedRewards;
            DailyPercentileRewards = dailyPercentileRewards;
            WeeklyRankedRewards = weeklyRankedRewards;
            WeeklyPercentileRewards = weeklyPercentileRewards;
            SeasonalRankedRewards = seasonalRankedRewards;
            SeasonalPercentileRewards = seasonalPercentileRewards;
            XPBooster = xPBooster;
            BPBooster = bPBooster;
            XPBoosterWipe = xPBoosterWipe;
            BPBoosterWipe = bPBoosterWipe;
        }
    }
}
