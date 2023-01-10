using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageAbility
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutAbility> Abilities { get; set; }

    public PageAbility(int pageID, Dictionary<int, LoadoutAbility> abilities)
    {
        PageID = pageID;
        Abilities = abilities;
    }
}