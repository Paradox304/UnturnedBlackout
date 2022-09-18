using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.Data
{
    public class KillstreakData
    {
        public int KillstreakID { get; set; }
        public ushort TriggerItemID { get; set; }
        public bool IsItem { get; set; }
        public ushort ItemID { get; set; }
        public bool RemoveWhenAmmoEmpty { get; set; }
        public int ItemStaySeconds { get; set; }

        public KillstreakData()
        {

        }
    }
}
