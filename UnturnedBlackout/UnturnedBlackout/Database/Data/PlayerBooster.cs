using Steamworks;
using System;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Data;

public class PlayerBooster
{
    public CSteamID SteamID { get; set; }
    public EBoosterType BoosterType { get; set; }
    public float BoosterValue { get; set; }
    public DateTimeOffset BoosterExpiration { get; set; }

    public PlayerBooster(CSteamID steamID, EBoosterType boosterType, float boosterValue, DateTimeOffset boosterExpiration)
    {
        SteamID = steamID;
        BoosterType = boosterType;
        BoosterValue = boosterValue;
        BoosterExpiration = boosterExpiration;
    }
}
