using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.UI
{
    public class PageGlove
    {
        public int PageID { get; set; }
        public Dictionary<int, PageGlove> Gloves { get; set; }

        public PageGlove(int pageID, Dictionary<int, PageGlove> gloves)
        {
            PageID = pageID;
            Gloves = gloves;
        }
    }
}
