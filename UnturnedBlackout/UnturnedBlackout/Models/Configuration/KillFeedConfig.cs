using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Models.Feed;

namespace UnturnedBlackout.Models.Configuration
{
    public class KillFeedConfig
    {
        public List<FeedIcon> KillFeedIcons { get; set; }

        public KillFeedConfig()
        {
            KillFeedIcons = new();
        }
    }
}
