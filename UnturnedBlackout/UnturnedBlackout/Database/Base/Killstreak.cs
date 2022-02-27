using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class Killstreak
    {
        public int KillstreakID { get; set; }
        public string KillstreakName { get; set; }
        public string IconLink { get; set; }
        public int KillstreakRequired { get; set; }
        public int BuyPrice { get; set; }
        public int ScrapAmount { get; set; }
        public bool IsDefault { get; set; }

        public Killstreak(int killstreakID, string killstreakName, string iconLink, int killstreakRequired, int buyPrice, int scrapAmount, bool isDefault)
        {
            KillstreakID = killstreakID;
            KillstreakName = killstreakName;
            IconLink = iconLink;
            KillstreakRequired = killstreakRequired;
            BuyPrice = buyPrice;
            ScrapAmount = scrapAmount;
            IsDefault = isDefault;
        }
    }
}
