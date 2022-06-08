namespace UnturnedBlackout.Database.Base
{
    public class Killstreak
    {
        public int KillstreakID { get; set; }
        public string KillstreakName { get; set; }
        public string KillstreakDesc { get; set; }
        public string KillstreakRarity { get; set; }
        public string IconLink { get; set; }
        public int KillstreakRequired { get; set; }
        public int BuyPrice { get; set; }
        public int Coins { get; set; }
        public int ScrapAmount { get; set; }
        public int LevelRequirement { get; set; }

        public Killstreak(int killstreakID, string killstreakName, string killstreakDesc, string killstreakRarity, string iconLink, int killstreakRequired, int buyPrice, int coins, int scrapAmount, int levelRequirement)
        {
            KillstreakID = killstreakID;
            KillstreakName = killstreakName;
            KillstreakDesc = killstreakDesc;
            KillstreakRarity = killstreakRarity;
            IconLink = iconLink;
            KillstreakRequired = killstreakRequired;
            BuyPrice = buyPrice;
            Coins = coins;
            ScrapAmount = scrapAmount;
            LevelRequirement = levelRequirement;
        }

        public int GetCoins(uint currentLevel)
        {
            var levelsNeeded = LevelRequirement - currentLevel;
            return Coins * (int)levelsNeeded;
        }
    }
}
