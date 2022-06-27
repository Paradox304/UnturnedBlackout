using System;
using System.Net;

namespace UnturnedBlackout.Database.Base
{
    public class Server
    {
        public string IP { get; set; }
        public string Port { get; set; }
        public string ServerName { get; set; }

        public uint IPNo { get; set; }
        public ushort PortNo { get; set; }

        public string FriendlyIP { get; set; }
        public string ServerBanner { get; set; }
        public string ServerDesc { get; set; }

        // Details got from the timer
        public string Name { get; set; }
        public int Players { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsOnline { get; set; }

        public Server(string iP, string port, string serverName, string friendlyIP, string serverBanner, string serverDesc)
        {
            IP = iP;
            Port = port;
            ServerName = serverName;
            FriendlyIP = friendlyIP;
            ServerBanner = serverBanner;
            ServerDesc = serverDesc;

            if (!IPAddress.TryParse(IP, out IPAddress ipAddress))
            {
                throw new ArgumentException("IP is not correct");
            }

            var ipBytes = ipAddress.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ipBytes);
            }
            IPNo = BitConverter.ToUInt32(ipBytes, 0);

            if (!ushort.TryParse(Port, out ushort portNo))
            {
                throw new ArgumentException("Port is not correct");
            }
            PortNo = portNo;
        }
    }
}
