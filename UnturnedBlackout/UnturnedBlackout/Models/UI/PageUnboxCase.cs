using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageUnboxCase
{
    public int PageID { get; set; }
    public Dictionary<int, PlayerCase> Cases { get; set; }

    public PageUnboxCase(int pageID, Dictionary<int, PlayerCase> cases)
    {
        PageID = pageID;
        Cases = cases;
    }
}