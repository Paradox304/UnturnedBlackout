using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class Server
    {
        public string IP { get; set; }
        public string Port { get; set; }
        public string ServerName { get; set; }
        
        public uint IPNo { get; set; }
        public ushort PortNo { get; set; }
        
        // Details got from the timer
        public string Name { get; set; }
        public int Players { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsOnline { get; set; }
        
        public Server(string iP, string port, string serverName)
        {
            IP = iP;
            Port = port;
            ServerName = serverName;

            if (!IPAddress.TryParse(IP, out IPAddress ipAddress))
            {
                throw new ArgumentException("IP is not correct");
            }
            
            var ipBytes = ipAddress.GetAddressBytes();
            var ip = (uint)ipBytes[3] << 24;
            ip += (uint)ipBytes[2] << 16;
            ip += (uint)ipBytes[1] << 8;
            ip += ipBytes[0];

            IPNo = ip;
            if (!ushort.TryParse(Port, out ushort portNo))
            {
                throw new ArgumentException("Port is not correct");
            }
            PortNo = portNo;
        }
    }
}
