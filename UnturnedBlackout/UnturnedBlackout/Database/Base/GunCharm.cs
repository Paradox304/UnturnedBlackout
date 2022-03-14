using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class GunCharm
    {
        public ushort CharmID { get; set; }
        public string CharmName { get; set; }
        public string CharmDesc { get; set; }
        public string IconLink { get; set; }
        public int BuyPrice { get; set; }
        public int Coins { get; set; }
        public int ScrapAmount { get; set; }
        public int LevelRequired { get; set; }

        public GunCharm(ushort charmID, string charmName, string charmDesc, string iconLink, int buyPrice, int coins, int scrapAmount, int levelRequired)
        {
            CharmID = charmID;
            CharmName = charmName;
            CharmDesc = charmDesc;
            IconLink = iconLink;
            BuyPrice = buyPrice;
            Coins = coins;
            ScrapAmount = scrapAmount;
            LevelRequired = levelRequired;
        }
    }
}
