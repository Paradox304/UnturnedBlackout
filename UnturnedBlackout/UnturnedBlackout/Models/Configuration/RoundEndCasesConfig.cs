using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class RoundEndCasesConfig
{
    public int Chance { get; set; }
    public int MinimumMinutesPlayed { get; set; }

    public List<RoundEndCase> RoundEndCases { get; set; }

    public RoundEndCasesConfig()
    {
        Chance = 0;
        MinimumMinutesPlayed = 0;
        RoundEndCases = new();
    }
}