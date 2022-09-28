using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.Global
{
    public class TeamKillstreak
    {
        public int KillstreakID { get; set; }
        public ushort ShirtID { get; set; }
        public ushort PantsID { get; set; }
        public ushort HatID { get; set; }
        public ushort VestID { get; set; }

        public TeamKillstreak()
        {

        }
    }
}
