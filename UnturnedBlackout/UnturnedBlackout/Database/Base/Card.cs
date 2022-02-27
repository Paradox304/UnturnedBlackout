using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class Card
    {
        public int CardID { get; set; }
        public string CardName { get; set; }
        public string CardDesc { get; set; }
        public string IconLink { get; set; }
        public string CardLink { get; set; }
        public int BuyAmount { get; set; }
        public int ScrapAmount { get; set; }
        public bool IsDefault { get; set; }

        public Card(int cardID, string cardName, string cardDesc, string iconLink, string cardLink, int buyAmount, int scrapAmount, bool isDefault)
        {
            CardID = cardID;
            CardName = cardName;
            CardDesc = cardDesc;
            IconLink = iconLink;
            CardLink = cardLink;
            BuyAmount = buyAmount;
            ScrapAmount = scrapAmount;
            IsDefault = isDefault;
        }
    }
}
