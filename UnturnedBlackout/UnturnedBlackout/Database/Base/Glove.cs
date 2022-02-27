using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class Glove
    {
        public ushort GloveID { get; set; }
        public string GloveName { get; set; }
        public string IconLink { get; set; }
        public int BuyPrice { get; set; }
        public int ScrapAmount { get; set; }
        public bool IsDefault { get; set; }

        public Glove(ushort gloveID, string gloveName, string iconLink, int buyPrice, int scrapAmount, bool isDefault)
        {
            GloveID = gloveID;
            GloveName = gloveName;
            IconLink = iconLink;
            BuyPrice = buyPrice;
            ScrapAmount = scrapAmount;
            IsDefault = isDefault;
        }
    }
}
