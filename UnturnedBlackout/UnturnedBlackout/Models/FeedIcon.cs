using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models
{
    public class FeedIcon
    {
        public ushort WeaponID { get; set; }
        public string Symbol { get; set; }

        public FeedIcon(ushort weaponID, string symbol)
        {
            WeaponID = weaponID;
            Symbol = symbol;
        }

        public FeedIcon()
        {

        }
    }
}
