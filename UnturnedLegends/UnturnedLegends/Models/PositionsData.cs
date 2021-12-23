using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.SpawnPoints;

namespace UnturnedLegends.Models
{
    public class PositionsData
    {
        public List<FFASpawnPoint> FFASpawnPoints { get; set; }

        public PositionsData()
        {
            FFASpawnPoints = new List<FFASpawnPoint>();
        }
    }
}
