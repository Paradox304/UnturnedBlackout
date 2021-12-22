using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedLegends.Models
{
    public class FFAConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerKillHeadshot { get; set; }

        public int BaseXPKS { get; set; }
        public int IncreaseXPPerKS { get; set; }

        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }


    }
}
