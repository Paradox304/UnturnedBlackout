using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class GunSkin
    {
        public int ID { get; set; }
        public Gun Gun { get; set; }
        public ushort SkinID { get; set; }
        public string SkinName { get; set; }
        public string SkinDesc { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }

        public GunSkin(int iD, Gun gun, ushort skinID, string skinName, string skinDesc, string iconLink, int scrapAmount)
        {
            ID = iD;
            Gun = gun;
            SkinID = skinID;
            SkinName = skinName;
            SkinDesc = skinDesc;
            IconLink = iconLink;
            ScrapAmount = scrapAmount;
        }
    }
}
