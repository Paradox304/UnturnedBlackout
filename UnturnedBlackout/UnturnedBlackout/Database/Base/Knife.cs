namespace UnturnedBlackout.Database.Base
{
    public class Knife
    {
        public ushort KnifeID { get; set; }
        public string KnifeName { get; set; }
        public string KnifeDesc { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }
        public int BuyPrice { get; set; }
        public bool IsDefault { get; set; }

        public Knife(ushort knifeID, string knifeName, string knifeDesc, string iconLink, int scrapAmount, int buyPrice, bool isDefault)
        {
            KnifeID = knifeID;
            KnifeName = knifeName;
            KnifeDesc = knifeDesc;
            IconLink = iconLink;
            ScrapAmount = scrapAmount;
            BuyPrice = buyPrice;
            IsDefault = isDefault;
        }
    }
}
