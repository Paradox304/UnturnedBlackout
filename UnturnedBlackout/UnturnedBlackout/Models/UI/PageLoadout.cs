using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageLoadout
{
    public int PageID { get; set; }
    public Dictionary<int, Loadout> Loadouts { get; set; }

    public PageLoadout(int pageID, Dictionary<int, Loadout> loadouts)
    {
        PageID = pageID;
        Loadouts = loadouts;
    }
}
