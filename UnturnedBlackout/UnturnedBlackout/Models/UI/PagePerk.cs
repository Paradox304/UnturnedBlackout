using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PagePerk
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutPerk> Perks { get; set; }

    public PagePerk(int pageID, Dictionary<int, LoadoutPerk> perks)
    {
        PageID = pageID;
        Perks = perks;
    }
}