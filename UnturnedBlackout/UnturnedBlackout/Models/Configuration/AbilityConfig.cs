using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class AbilitiesConfig
{
    public List<AbilityData> AbilitiesData { get; set; }

    public AbilitiesConfig()
    {
        AbilitiesData = new();
    }
}