using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnturnedLegends.SpawnPoints
{
    public class TDMSpawnPoint
    {
        public int LocationID { get; set; }
        public int TeamID { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public TDMSpawnPoint(int locationID, int teamID, float x, float y, float z)
        {
            LocationID = locationID;
            TeamID = teamID;
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3 GetSpawnPoint()
        {
            return new Vector3(X, Y, Z);
        }
    }
}
