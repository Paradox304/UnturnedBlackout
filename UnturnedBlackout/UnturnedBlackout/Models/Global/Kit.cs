using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.Global
{
    public class Kit
    {
        public List<ushort> ItemIDs { get; set; }

        public Kit()
        {

        }

        public Kit(List<ushort> itemIDs)
        {
            ItemIDs = itemIDs;
        }
    }
}
