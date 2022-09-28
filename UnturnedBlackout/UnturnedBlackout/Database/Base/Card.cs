
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Card
    {
        public int CardID { get; set; }
        public string CardName { get; set; }
        public string CardDesc { get; set; }
        public ERarity CardRarity { get; set; }
        public string IconLink { get; set; }
        public string CardLink { get; set; }
        public int ScrapAmount { get; set; }
        public int BuyPrice { get; set; }
        public int Coins { get; set; }
        public int LevelRequirement { get; set; }
        public string AuthorCredits { get; set; }

        public Card(int cardID, string cardName, string cardDesc, ERarity cardRarity, string iconLink, string cardLink, int scrapAmount, int buyPrice, int coins, int levelRequirement, string authorCredits)
        {
            CardID = cardID;
            CardName = cardName;
            CardDesc = cardDesc;
            CardRarity = cardRarity;
            IconLink = iconLink;
            CardLink = cardLink;
            ScrapAmount = scrapAmount;
            BuyPrice = buyPrice;
            Coins = coins;
            LevelRequirement = levelRequirement;
            AuthorCredits = authorCredits;
        }

        public int GetCoins(int currentLevel)
        {
            var levelsNeeded = LevelRequirement - currentLevel;
            var coinsRequired = Coins * levelsNeeded;
            return coinsRequired > 0 ? coinsRequired : 0;
        }
    }
}
