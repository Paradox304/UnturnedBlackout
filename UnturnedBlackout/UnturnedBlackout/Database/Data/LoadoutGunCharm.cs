using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutGunCharm
    {
        public GunCharm GunCharm { get; set; }
        public bool IsBought { get; set; }

        public LoadoutGunCharm(GunCharm gunCharm, bool isBought)
        {
            GunCharm = gunCharm;
            IsBought = isBought;
        }
    }
}
