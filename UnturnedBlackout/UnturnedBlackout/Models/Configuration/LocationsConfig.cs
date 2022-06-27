using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration
{
    public class LocationsConfig
    {
        public List<ArenaLocation> ArenaLocations { get; set; }

        public LocationsConfig()
        {
            ArenaLocations = new();
        }
    }
}
