using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Killstreak
    {
        public int KillstreakID { get; set; }
        public string KillstreakName { get; set; }
        public string IconLink { get; set; }
        public EKillstreak KillstreakType { get; set; }
        public int KillstreakRequired { get; set; }
        public int BuyPrice { get; set; }
        public int ScrapAmount { get; set; }
        public bool IsDefault { get; set; }

        public Killstreak(int killstreakID, string killstreakName, string iconLink, EKillstreak killstreakType, int killstreakRequired, int buyPrice, int scrapAmount, bool isDefault)
        {
            KillstreakID = killstreakID;
            KillstreakName = killstreakName;
            IconLink = iconLink;
            KillstreakType = killstreakType;
            KillstreakRequired = killstreakRequired;
            BuyPrice = buyPrice;
            ScrapAmount = scrapAmount;
            IsDefault = isDefault;
        }
    }
}
