namespace UnturnedBlackout.Database.Base
{
    public class Glove
    {
        public ushort GloveID { get; set; }
        public string GloveName { get; set; }
        public string GloveDesc { get; set; }
        public string GloveRarity { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }
        public int BuyPrice { get; set; }
        public int Coins { get; set; }
        public int LevelRequirement { get; set; }

        public Glove(ushort gloveID, string gloveName, string gloveDesc, string gloveRarity, string iconLink, int scrapAmount, int buyPrice, int coins, int levelRequirement)
        {
            GloveID = gloveID;
            GloveName = gloveName;
            GloveDesc = gloveDesc;
            GloveRarity = gloveRarity;
            IconLink = iconLink;
            ScrapAmount = scrapAmount;
            BuyPrice = buyPrice;
            Coins = coins;
            LevelRequirement = levelRequirement;
        }
    }
}
