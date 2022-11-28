using System;
using UnityEngine;

namespace UnturnedBlackout.Models.Global;

[Serializable]
public class LobbySpawn
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Yaw { get; set; }

    public LobbySpawn()
    {
        
    }
    
    public LobbySpawn(float x, float y, float z, float yaw)
    {
        X = x;
        Y = y;
        Z = z;
        Yaw = yaw;
    }

    public Vector3 GetSpawnPoint() => new Vector3(X, Y, Z);
}