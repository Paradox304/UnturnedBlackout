using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageGadget
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutGadget> Gadgets { get; set; }

    public PageGadget(int pageID, Dictionary<int, LoadoutGadget> gadgets)
    {
        PageID = pageID;
        Gadgets = gadgets;
    }
}