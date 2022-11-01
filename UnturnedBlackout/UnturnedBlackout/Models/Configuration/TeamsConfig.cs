using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class TeamsConfig
{
    public List<TeamInfo> TeamsInfo { get; set; }

    public TeamsConfig()
    {
        TeamsInfo = new();
    }
}