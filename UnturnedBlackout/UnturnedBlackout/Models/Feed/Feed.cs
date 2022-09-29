using System;

namespace UnturnedBlackout.Models.Feed;

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