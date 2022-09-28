using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageKnife
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutKnife> Knives { get; set; }

    public PageKnife(int pageID, Dictionary<int, LoadoutKnife> knives)
    {
        PageID = pageID;
        Knives = knives;
    }
}
