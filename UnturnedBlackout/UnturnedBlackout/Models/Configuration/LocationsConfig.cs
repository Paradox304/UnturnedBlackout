using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class LocationsConfig
{
    public List<ArenaLocation> ArenaLocations { get; set; }

    public LocationsConfig()
    {
        ArenaLocations = new();
    }
}