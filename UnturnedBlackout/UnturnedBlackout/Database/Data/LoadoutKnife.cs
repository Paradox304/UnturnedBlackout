using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutKnife
    {
        public Knife Knife { get; set; }
        public int KnifeKills { get; set; }
        public bool IsBought { get; set; }

        public LoadoutKnife(Knife knife, int knifeKills, bool isBought)
        {
            Knife = knife;
            KnifeKills = knifeKills;
            IsBought = isBought;
        }
    }
}
