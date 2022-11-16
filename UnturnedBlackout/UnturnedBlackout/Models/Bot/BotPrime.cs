using System;

namespace UnturnedBlackout.Models.Bot;

[Serializable]
public class BotPrime
{
    public string SteamID { get; set; }

    public BotPrime(string steamID)
    {
        SteamID = steamID;
    }
}