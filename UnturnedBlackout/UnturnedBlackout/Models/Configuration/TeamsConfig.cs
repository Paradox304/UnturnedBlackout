using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration
{
    public class TeamsConfig
    {
        public List<TeamInfo> TeamsInfo { get; set; }

        public TeamsConfig()
        {
            TeamsInfo = new();
        }
    }
}
