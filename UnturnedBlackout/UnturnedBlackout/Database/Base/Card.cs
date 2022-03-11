namespace UnturnedBlackout.Database.Base
{
    public class Card
    {
        public int CardID { get; set; }
        public string CardName { get; set; }
        public string CardDesc { get; set; }
        public string IconLink { get; set; }
        public string CardLink { get; set; }
        public int BuyPrice { get; set; }
        public int ScrapAmount { get; set; }
        public bool IsDefault { get; set; }

        public Card(int cardID, string cardName, string cardDesc, string iconLink, string cardLink, int buyPrice, int scrapAmount, bool isDefault)
        {
            CardID = cardID;
            CardName = cardName;
            CardDesc = cardDesc;
            IconLink = iconLink;
            CardLink = cardLink;
            BuyPrice = buyPrice;
            ScrapAmount = scrapAmount;
            IsDefault = isDefault;
        }
    }
}
