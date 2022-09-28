using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageGlove
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutGlove> Gloves { get; set; }

    public PageGlove(int pageID, Dictionary<int, LoadoutGlove> gloves)
    {
        PageID = pageID;
        Gloves = gloves;
    }
}
