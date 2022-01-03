﻿using System.Collections.Generic;

namespace UnturnedLegends.Models
{
    public class PositionsData
    {
        public List<FFASpawnPoint> FFASpawnPoints { get; set; }
        public List<TDMSpawnPoint> TDMSpawnPoints { get; set; }

        public PositionsData()
        {
            FFASpawnPoints = new List<FFASpawnPoint>();
            TDMSpawnPoints = new List<TDMSpawnPoint>();
        }
    }
}
