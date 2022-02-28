using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutGun
    {
        public Gun Gun { get; set; }
        public int Level { get; set; }
        public int XP { get; set; }
        public int GunKills { get; set; }
        public bool IsBought { get; set; }
        public List<LoadoutAttachment> Attachments { get; set; }

        public LoadoutGun(Gun gun, int level, int xP, int gunKills, bool isBought, List<LoadoutAttachment> attachments)
        {
            Gun = gun;
            Level = level;
            XP = xP;
            GunKills = gunKills;
            IsBought = isBought;
            Attachments = attachments;
        }
    }
}
