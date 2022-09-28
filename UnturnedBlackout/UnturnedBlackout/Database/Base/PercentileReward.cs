using System.Collections.Generic;

namespace UnturnedBlackout.Database.Base;

public class PercentileReward
{
    public int LowerPercentile { get; set; }
    public int UpperPercentile { get; set; }

    public List<Reward> Rewards { get; set; }

    public PercentileReward(int lowerPercentile, int upperPercentile, List<Reward> rewards)
    {
        LowerPercentile = lowerPercentile;
        UpperPercentile = upperPercentile;
        Rewards = rewards;
    }
}
