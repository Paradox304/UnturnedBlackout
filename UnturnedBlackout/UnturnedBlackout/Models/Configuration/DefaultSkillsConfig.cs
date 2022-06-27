using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration
{
    public class DefaultSkillsConfig
    {
        public List<DefaultSkill> DefaultSkills { get; set; }

        public DefaultSkillsConfig()
        {
            DefaultSkills = new();
        }
    }
}
