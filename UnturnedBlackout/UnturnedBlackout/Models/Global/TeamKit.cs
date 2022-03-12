using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.Global
{
    public class TeamKit
    {
        public List<ushort> ItemIDs { get; set; }

        public TeamKit()
        {

        }

        public TeamKit(List<ushort> itemIDs)
        {
            ItemIDs = itemIDs;
        }
    }
}
