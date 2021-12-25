using System.Collections.Generic;
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
