using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class DefaultSkillsConfig
{
    public List<DefaultSkill> DefaultSkills { get; set; }

    public DefaultSkillsConfig()
    {
        DefaultSkills = new();
    }
}