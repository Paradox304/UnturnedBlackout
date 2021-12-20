using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedLegends.Models
{
    public class ArenaLocation
    {
        public int LocationID { get; set; }
        public string LocationName { get; set; }

        public ArenaLocation()
        {

        }

        public ArenaLocation(int locationID, string locationName)
        {
            LocationID = locationID;
            LocationName = locationName;
        }
    }
}
