using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration;

public class TeamsConfig
{
    public List<TeamInfo> TeamsInfo { get; set; }

    public TeamsConfig()
    {
        TeamsInfo = new();
    }
}