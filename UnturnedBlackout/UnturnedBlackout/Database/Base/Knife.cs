namespace UnturnedBlackout.Database.Base
{
    public class Knife
    {
        public ushort KnifeID { get; set; }
        public string KnifeName { get; set; }
        public string KnifeDesc { get; set; }
        public string KnifeRarity { get; set; }
        public float MovementChange { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }
        public int Coins { get; set; }
        public int BuyPrice { get; set; }
        public int LevelRequirement { get; set; }

        public Knife(ushort knifeID, string knifeName, string knifeDesc, string knifeRarity, float movementChange, string iconLink, int scrapAmount, int coins, int buyPrice, int levelRequirement)
        {
            KnifeID = knifeID;
            KnifeName = knifeName;
            KnifeDesc = knifeDesc;
            KnifeRarity = knifeRarity;
            MovementChange = movementChange;
            IconLink = iconLink;
            ScrapAmount = scrapAmount;
            Coins = coins;
            BuyPrice = buyPrice;
            LevelRequirement = levelRequirement;
        }

        public int GetCoins(uint currentLevel)
        {
            var levelsNeeded = LevelRequirement - currentLevel;
            return Coins * (int)levelsNeeded;
        }
    }
}
