using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models
{
    public class LevelXP
    {
        public uint Level { get; set; }
        public int XPNeeded { get; set; }

        public LevelXP()
        {

        }

        public LevelXP(uint level, int xPNeeded)
        {
            Level = level;
            XPNeeded = xPNeeded;
        }
    }
}
