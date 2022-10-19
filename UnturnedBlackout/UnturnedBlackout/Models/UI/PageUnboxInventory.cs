using System.Collections.Generic;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;

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