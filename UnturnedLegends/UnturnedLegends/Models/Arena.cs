using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace UnturnedLegends.Models
{
    public class Arena
    {
        public int ArenaID { get; set; }
        public int MaxPlayers { get; set; }
        [XmlArrayItem("LocationID")]
        public List<int> Locations { get; set; }

        public Arena()
        {

        }

        public Arena(int arenaID, int maxPlayers, List<int> locations)
        {
            ArenaID = arenaID;
            MaxPlayers = maxPlayers;
            Locations = locations;
        }
    }
}
