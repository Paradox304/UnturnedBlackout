using System.Collections.Generic;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Models.UI;

public class PageBattlepass
{
    public int PageID { get; set; }
    public Dictionary<int, BattlepassTier> Tiers { get; set; }
    public Dictionary<int, int> TiersInverse { get; set; }

    public PageBattlepass(int pageID, Dictionary<int, BattlepassTier> tiers, Dictionary<int, int> tiersInverse)
    {
        PageID = pageID;
        Tiers = tiers;
        TiersInverse = tiersInverse;
    }
}