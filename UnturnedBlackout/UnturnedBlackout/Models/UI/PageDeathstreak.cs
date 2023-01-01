using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageDeathstreak
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutDeathstreak> Deathstreaks { get; set; }

    public PageDeathstreak(int pageID, Dictionary<int, LoadoutDeathstreak> deathstreaks)
    {
        PageID = pageID;
        Deathstreaks = deathstreaks;
    }
}