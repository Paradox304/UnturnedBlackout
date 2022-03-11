using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI
{
    public class PageCard
    {
        public int PageID { get; set; }
        public Dictionary<int, LoadoutCard> Cards { get; set; }

        public PageCard(int pageID, Dictionary<int, LoadoutCard> cards)
        {
            PageID = pageID;
            Cards = cards;
        }
    }
}
