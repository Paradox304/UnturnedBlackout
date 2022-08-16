using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Models.UI
{
    public class PageUnboxStore
    {
        public int PageID { get; set; }
        public Dictionary<int, Case> Cases { get; set; }

        public PageUnboxStore(int pageID, Dictionary<int, Case> cases)
        {
            PageID = pageID;
            Cases = cases;
        }
    }
}
