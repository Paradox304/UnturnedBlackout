namespace UnturnedBlackout.Database.Base
{
    public class GunSkin
    {
        public int ID { get; set; }
        public Gun Gun { get; set; }
        public ushort SkinID { get; set; }
        public string SkinName { get; set; }
        public string SkinDesc { get; set; }
        public string SkinRarity { get; set; }
        public string PatternLink { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }

        public GunSkin(int iD, Gun gun, ushort skinID, string skinName, string skinDesc, string skinRarity, string patternLink, string iconLink, int scrapAmount)
        {
            ID = iD;
            Gun = gun;
            SkinID = skinID;
            SkinName = skinName;
            SkinDesc = skinDesc;
            SkinRarity = skinRarity;
            PatternLink = patternLink;
            IconLink = iconLink;
            ScrapAmount = scrapAmount;
        }
    }
}
