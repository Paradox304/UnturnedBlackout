using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class RoundEndCasesConfig
{
    public float Chance { get; set; }
    public float PrimeBonusMultiplier { get; set; }
    public int MinimumMinutesPlayed { get; set; }

    public List<RoundEndCase> RoundEndCases { get; set; }

    public RoundEndCasesConfig()
    {
        RoundEndCases = new();
    }
}