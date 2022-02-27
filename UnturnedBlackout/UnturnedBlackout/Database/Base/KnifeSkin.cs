using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class KnifeSkin
    {
        public int ID { get; set; }
        public Knife Knife { get; set; }
        public ushort SkinID { get; set; }
        public string SkinName { get; set; }
        public string SkinDesc { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }

        public KnifeSkin(int iD, Knife knife, ushort skinID, string skinName, string skinDesc, string iconLink, int scrapAmount)
        {
            ID = iD;
            Knife = knife;
            SkinID = skinID;
            SkinName = skinName;
            SkinDesc = skinDesc;
            IconLink = iconLink;
            ScrapAmount = scrapAmount;
        }
    }
}
