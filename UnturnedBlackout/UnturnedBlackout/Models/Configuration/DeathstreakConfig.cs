using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class DeathstreakConfig
{
    public List<DeathstreakData> DeathstreaksData { get; set; }

    public DeathstreakConfig()
    {
        DeathstreaksData = new();
    }
}