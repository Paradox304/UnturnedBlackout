using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI
{
    public class PageGun
    {
        public int PageID { get; set; }
        public Dictionary<int, LoadoutGun> Guns { get; set; }

        public PageGun(int pageID, Dictionary<int, LoadoutGun> guns)
        {
            PageID = pageID;
            Guns = guns;
        }
    }
}
