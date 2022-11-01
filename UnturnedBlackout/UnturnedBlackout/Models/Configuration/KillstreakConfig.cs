using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class KillstreakConfig
{
    public List<KillstreakData> KillstreaksData { get; set; }

    public KillstreakConfig()
    {
        KillstreaksData = new();
    }
}