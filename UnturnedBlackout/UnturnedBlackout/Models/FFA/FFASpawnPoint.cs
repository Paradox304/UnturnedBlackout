using UnityEngine;

namespace UnturnedBlackout.Models.FFA;

public class FFASpawnPoint
{
    public int LocationID { get; set; }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public float Yaw { get; set; }

    public FFASpawnPoint()
    {
    }

    public FFASpawnPoint(int locationID, float x, float y, float z, float yaw)
    {
        LocationID = locationID;
        X = x;
        Y = y;
        Z = z;
        Yaw = yaw;
    }

    public Vector3 GetSpawnPoint() => new(X, Y, Z);
}