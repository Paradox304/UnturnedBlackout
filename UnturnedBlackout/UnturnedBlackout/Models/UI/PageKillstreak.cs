using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.UI
{
    public class PageKillstreak
    {
        public int PageID { get; set; }
        public Dictionary<int, PageKillstreak> Killstreaks { get; set; }

        public PageKillstreak(int pageID, Dictionary<int, PageKillstreak> killstreaks)
        {
            PageID = pageID;
            Killstreaks = killstreaks;
        }
    }
}
