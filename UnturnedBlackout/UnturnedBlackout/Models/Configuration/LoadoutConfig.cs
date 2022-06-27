using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration
{
    public class LoadoutConfig
    {
        public int DefaultLoadoutAmount { get; set; }
        public List<LoadoutAmount> LoadoutAmounts { get; set; }

        public LoadoutConfig()
        {
            DefaultLoadoutAmount = 5;
            LoadoutAmounts = new();
        }
    }
}
