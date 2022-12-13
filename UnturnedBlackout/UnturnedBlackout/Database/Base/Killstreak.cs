using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Database.Base;

public class Killstreak
{
    public int KillstreakID { get; set; }
    public string KillstreakName { get; set; }
    public string KillstreakDesc { get; set; }
    public ERarity KillstreakRarity { get; set; }
    public string IconLink { get; set; }
    public int KillstreakRequired { get; set; }
    public int BuyPrice { get; set; }
    public int Coins { get; set; }
    public int ScrapAmount { get; set; }
    public int LevelRequirement { get; set; }
    public KillstreakData KillstreakInfo { get; set; }

    public Killstreak(int killstreakID, string killstreakName, string killstreakDesc, ERarity killstreakRarity, string iconLink, int killstreakRequired, int buyPrice, int coins, int scrapAmount, int levelRequirement, KillstreakData killstreakInfo)
    {
        KillstreakID = killstreakID;
        KillstreakName = killstreakName;
        KillstreakDesc = killstreakDesc;
        KillstreakRarity = killstreakRarity;
        IconLink = iconLink;
        KillstreakRequired = killstreakRequired;
        BuyPrice = buyPrice;
        Coins = coins;
        ScrapAmount = scrapAmount;
        LevelRequirement = levelRequirement;
        KillstreakInfo = killstreakInfo;
    }

    public int GetCoins(int currentLevel) => Coins;
}