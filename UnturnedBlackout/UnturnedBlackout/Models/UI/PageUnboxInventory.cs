using System.Collections.Generic;

namespace UnturnedBlackout.Models.UI;

public class PageUnboxInventory
{
    public int PageID { get; set; }
    public Dictionary<int, object> Skins { get; set; }

    public PageUnboxInventory(int pageID, Dictionary<int, object> skins)
    {
        PageID = pageID;
        Skins = skins;
    }
}