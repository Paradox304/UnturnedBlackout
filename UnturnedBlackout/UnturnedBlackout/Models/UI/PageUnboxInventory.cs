using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI
{
    public class PageUnboxInventory
    {
        public int PageID { get; set; }
        public Dictionary<int, PlayerCase> Cases { get; set; }

        public PageUnboxInventory(int pageID, Dictionary<int, PlayerCase> cases)
        {
            PageID = pageID;
            Cases = cases;
        }
    }
}
