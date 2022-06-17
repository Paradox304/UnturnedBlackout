using System;
using System.Collections.Generic;
using System.Linq;
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

        public Server(string iP, string port, string serverName)
        {
            IP = iP;
            Port = port;
            ServerName = serverName;
            
            if (!uint.TryParse(IP.Replace('.', ' '), out uint ipNo))
            {
                throw new ArgumentException("IP is not correct");
            }

            IPNo = ipNo;
            if (!ushort.TryParse(Port, out ushort portNo))
            {
                throw new ArgumentException("Port is not correct");
            }
            PortNo = portNo;
        }
    }
}
