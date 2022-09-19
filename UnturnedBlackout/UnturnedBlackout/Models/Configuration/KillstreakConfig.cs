using System.Collections.Generic;
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
