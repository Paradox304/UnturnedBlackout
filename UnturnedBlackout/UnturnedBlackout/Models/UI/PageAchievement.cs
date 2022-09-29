using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageAchievement
{
    public int PageID { get; set; }
    public Dictionary<int, PlayerAchievement> Achievements { get; set; }

    public PageAchievement(int pageID, Dictionary<int, PlayerAchievement> achievements)
    {
        PageID = pageID;
        Achievements = achievements;
    }
}