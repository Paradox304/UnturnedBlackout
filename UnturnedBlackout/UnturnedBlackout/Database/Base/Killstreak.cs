namespace UnturnedBlackout.Database.Base
{
    public class Killstreak
    {
        public int KillstreakID { get; set; }
        public string KillstreakName { get; set; }
        public string KillstreakDesc { get; set; }
        public string IconLink { get; set; }
        public int KillstreakRequired { get; set; }
        public int BuyPrice { get; set; }
        public int ScrapAmount { get; set; }
        public bool IsDefault { get; set; }

        public Killstreak(int killstreakID, string killstreakName, string killstreakDesc, string iconLink, int killstreakRequired, int buyPrice, int scrapAmount, bool isDefault)
        {
            KillstreakID = killstreakID;
            KillstreakName = killstreakName;
            KillstreakDesc = killstreakDesc;
            IconLink = iconLink;
            KillstreakRequired = killstreakRequired;
            BuyPrice = buyPrice;
            ScrapAmount = scrapAmount;
            IsDefault = isDefault;
        }
    }
}
