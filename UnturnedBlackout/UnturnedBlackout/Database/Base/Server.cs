using SDG.Unturned;
using System;
using System.Net;

namespace UnturnedBlackout.Database.Base;

public class Server
{
    public int ServerID { get; set; }
    public string IP { get; set; }
    public string Port { get; set; }
    public string ServerName { get; set; }
    public string FriendlyIP { get; set; }
    public string ServerDesc { get; set; }

    public int MaxPlayers { get; set; }
    public int Players { get; set; }
    public string CurrentServerName { get; set; }
    public float SurgeMultiplier { get; set; }
    public DateTimeOffset SurgeExpiry { get; set; }
    public DateTimeOffset LastUpdated { get; set; }


    public bool IsCurrentServer { get; set; }
    
    public uint IPNo { get; set; }
    public ushort PortNo { get; set; }

    public bool IsOnline => (DateTimeOffset.UtcNow - LastUpdated).TotalSeconds < 20;

    public Server(int serverID, string ip, string port, string serverName, string friendlyIP, string serverDesc, int maxPlayers, int players, string currentServerName, float surgeMultiplier, DateTimeOffset surgeExpiry, DateTimeOffset lastUpdated, bool isCurrentServer, uint ipNo, ushort portNo)
    {
        ServerID = serverID;
        IP = ip;
        Port = port;
        ServerName = serverName;
        FriendlyIP = friendlyIP;
        ServerDesc = serverDesc;
        MaxPlayers = maxPlayers;
        Players = players;
        CurrentServerName = currentServerName;
        SurgeMultiplier = surgeMultiplier;
        SurgeExpiry = surgeExpiry;
        LastUpdated = lastUpdated;
        IsCurrentServer = isCurrentServer;
        IPNo = ipNo;
        PortNo = portNo;
    }
}