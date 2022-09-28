using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageGunCharm
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutGunCharm> GunCharms { get; set; }

    public PageGunCharm(int pageID, Dictionary<int, LoadoutGunCharm> gunCharms)
    {
        PageID = pageID;
        GunCharms = gunCharms;
    }
}
