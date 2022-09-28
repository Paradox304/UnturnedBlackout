using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageKillstreak
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutKillstreak> Killstreaks { get; set; }

    public PageKillstreak(int pageID, Dictionary<int, LoadoutKillstreak> killstreaks)
    {
        PageID = pageID;
        Killstreaks = killstreaks;
    }
}
