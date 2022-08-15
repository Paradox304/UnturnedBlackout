using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class GunSkin
    {
        public int ID { get; set; }
        public Gun Gun { get; set; }
        public ushort SkinID { get; set; }
        public string SkinName { get; set; }
        public string SkinDesc { get; set; }
        public ERarity SkinRarity { get; set; }
        public string PatternLink { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }
        public int MaxAmount { get; set; }
        public int UnboxedAmount { get; set; }

        public GunSkin(int iD, Gun gun, ushort skinID, string skinName, string skinDesc, ERarity skinRarity, string patternLink, string iconLink, int scrapAmount, int maxAmount, int unboxedAmount)
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
            MaxAmount = maxAmount;
            UnboxedAmount = unboxedAmount;
        }
    }
}
