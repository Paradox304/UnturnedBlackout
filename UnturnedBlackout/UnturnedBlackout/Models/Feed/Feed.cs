using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.Feed
{
    public class Feed
    {
        public string KillMessage { get; set; }
        public DateTime Time { get; set; }

        public Feed(string killMessage, DateTime time)
        {
            KillMessage = killMessage;
            Time = time;
        }
    }
}
