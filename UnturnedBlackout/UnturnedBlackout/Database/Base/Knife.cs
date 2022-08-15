using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Knife
    {
        public ushort KnifeID { get; set; }
        public string KnifeName { get; set; }
        public string KnifeDesc { get; set; }
        public ERarity KnifeRarity { get; set; }
        public float MovementChange { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }
        public int Coins { get; set; }
        public int BuyPrice { get; set; }
        public int LevelRequirement { get; set; }
        public int KnifeWeight { get; set; }
        public int MaxAmount { get; set; }
        public int UnboxedAmount { get; set; }

        public Knife(ushort knifeID, string knifeName, string knifeDesc, ERarity knifeRarity, float movementChange, string iconLink, int scrapAmount, int coins, int buyPrice, int levelRequirement, int knifeWeight, int maxAmount, int unboxedAmount)
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
            KnifeWeight = knifeWeight;
            MaxAmount = maxAmount;
            UnboxedAmount = unboxedAmount;
        }

        public int GetCoins(int currentLevel)
        {
            var levelsNeeded = LevelRequirement - currentLevel;
            return Coins * levelsNeeded;
        }
    }
}
