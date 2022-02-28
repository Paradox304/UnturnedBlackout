using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutGlove
    {
        public Glove Glove { get; set; }
        public bool IsBought { get; set; }

        public LoadoutGlove(Glove glove, bool isBought)
        {
            Glove = glove;
            IsBought = isBought;
        }
    }
}
