using UnityEngine;

namespace UnturnedBlackout.Models.TDM
{
    public class TDMSpawnPoint
    {
        public int LocationID { get; set; }
        public int GroupID { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public TDMSpawnPoint(int locationID, int groupID, float x, float y, float z)
        {
            LocationID = locationID;
            GroupID = groupID;
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
