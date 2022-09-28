
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Glove
    {
        public int GloveID { get; set; }
        public string GloveName { get; set; }
        public string GloveDesc { get; set; }
        public ERarity GloveRarity { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }
        public int BuyPrice { get; set; }
        public int Coins { get; set; }
        public int LevelRequirement { get; set; }
        public int GloveWeight { get; set; }
        public int MaxAmount { get; set; }
        public int UnboxedAmount { get; set; }

        public Glove(int gloveID, string gloveName, string gloveDesc, ERarity gloveRarity, string iconLink, int scrapAmount, int buyPrice, int coins, int levelRequirement, int gloveWeight, int maxAmount, int unboxedAmount)
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
            GloveWeight = gloveWeight;
            MaxAmount = maxAmount;
            UnboxedAmount = unboxedAmount;
        }

        public int GetCoins(int currentLevel)
        {
            var levelsNeeded = LevelRequirement - currentLevel;
            var coinsRequired = Coins * levelsNeeded;
            return coinsRequired > 0 ? coinsRequired : 0;
        }
    }
}
