using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutPerk
    {
        public Perk Perk { get; set; }
        public bool IsBought { get; set; }

        public LoadoutPerk(Perk perk, bool isBought)
        {
            Perk = perk;
            IsBought = isBought;
        }
    }
}
