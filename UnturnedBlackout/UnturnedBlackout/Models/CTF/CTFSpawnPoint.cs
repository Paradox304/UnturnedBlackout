using UnityEngine;

namespace UnturnedBlackout.Models.CTF
{
    public class CTFSpawnPoint
    {
        public int LocationID { get; set; }
        public int GroupID { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float Yaw { get; set; }
        public bool IsFlagSP { get; set; }

        public CTFSpawnPoint()
        {

        }

        public CTFSpawnPoint(int locationID, int groupID, float x, float y, float z, float yaw, bool isFlagSP)
        {
            LocationID = locationID;
            GroupID = groupID;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            IsFlagSP = isFlagSP;
        }

        public Vector3 GetSpawnPoint()
        {
            return new Vector3(X, Y, Z);
        }
    }
}
