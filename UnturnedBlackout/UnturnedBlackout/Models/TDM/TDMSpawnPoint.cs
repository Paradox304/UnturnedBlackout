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

        public float Yaw { get; set; }

        public TDMSpawnPoint()
        {

        }

        public TDMSpawnPoint(int locationID, int groupID, float x, float y, float z, float yaw)
        {
            LocationID = locationID;
            GroupID = groupID;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
        }

        public Vector3 GetSpawnPoint()
        {
            return new Vector3(X, Y, Z);
        }
    }
}
