using System.Collections.Generic;

namespace UnturnedBlackout.Models.Global;

public class TeamInfo
{
    public int TeamID { get; set; }
    public string TeamName { get; set; }
    public string TeamColorHexCode { get; set; }
    public string KillFeedHexCode { get; set; }
    public string ChatPlayerHexCode { get; set; }
    public string ChatMessageHexCode { get; set; }
    public List<Kit> TeamKits { get; set; }
    public List<TeamGlove> TeamGloves { get; set; }
    public List<TeamKillstreak> TeamKillstreaks { get; set; }

    public TeamInfo()
    {
    }
}