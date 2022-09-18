using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Models.Configuration
{
    public class KillstreakConfig
    {
        public List<KillstreakData> KillstreaksData { get; set; }

        public KillstreakConfig()
        {
            KillstreaksData = new();
        }
    }
}
