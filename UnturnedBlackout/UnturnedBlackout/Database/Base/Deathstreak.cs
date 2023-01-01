using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Database.Base;

public class Deathstreak
{
    public int DeathstreakID { get; set; }
    public string DeathstreakName { get; set; }
    public string DeathstreakDesc { get; set; }
    public ERarity DeathstreakRarity { get; set; }
    public string IconLink { get; set; }
    public int DeathstreakRequired { get; set; }
    public int BuyPrice { get; set; }
    public int Coins { get; set; }
    public int ScrapAmount { get; set; }
    public int LevelRequirement { get; set; }
    public DeathstreakData DeathstreakInfo { get; set; }

    public Deathstreak(int deathstreakID, string deathstreakName, string deathstreakDesc, ERarity deathstreakRarity, string iconLink, int deathstreakRequired, int buyPrice, int coins, int scrapAmount, int levelRequirement, DeathstreakData deathstreakInfo)
    {
        DeathstreakID = deathstreakID;
        DeathstreakName = deathstreakName;
        DeathstreakDesc = deathstreakDesc;
        DeathstreakRarity = deathstreakRarity;
        IconLink = iconLink;
        DeathstreakRequired = deathstreakRequired;
        BuyPrice = buyPrice;
        Coins = coins;
        ScrapAmount = scrapAmount;
        LevelRequirement = levelRequirement;
        DeathstreakInfo = deathstreakInfo;
    }

    public int GetCoins(int currentLevel) => Coins;
}