namespace UnturnedBlackout.Database.Base
{
    public class GunCharm
    {
        public ushort CharmID { get; set; }
        public string CharmName { get; set; }
        public string CharmDesc { get; set; }
        public string CharmRarity { get; set; }
        public string IconLink { get; set; }
        public int BuyPrice { get; set; }
        public int Coins { get; set; }
        public int ScrapAmount { get; set; }
        public int LevelRequirement { get; set; }

        public GunCharm(ushort charmID, string charmName, string charmDesc, string charmRarity, string iconLink, int buyPrice, int coins, int scrapAmount, int levelRequirement)
        {
            CharmID = charmID;
            CharmName = charmName;
            CharmDesc = charmDesc;
            CharmRarity = charmRarity;
            IconLink = iconLink;
            BuyPrice = buyPrice;
            Coins = coins;
            ScrapAmount = scrapAmount;
            LevelRequirement = levelRequirement;
        }
    }
}
