using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.Global
{
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
}
