using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Feed;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class KillFeedConfig
{
    public List<FeedIcon> KillFeedIcons { get; set; }

    public KillFeedConfig()
    {
        KillFeedIcons = new();
    }
}