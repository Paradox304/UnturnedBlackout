using System;

namespace UnturnedBlackout.Models.Global;

[Serializable]
public class DefaultSkill
{
    public string SkillName { get; set; }
    public int SkillLevel { get; set; }

    public DefaultSkill()
    {
    }

    public DefaultSkill(string skillName, int skillLevel)
    {
        SkillName = skillName;
        SkillLevel = skillLevel;
    }
}