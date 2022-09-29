using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base;

public class GunCharm
{
    public ushort CharmID { get; set; }
    public string CharmName { get; set; }
    public string CharmDesc { get; set; }
    public ERarity CharmRarity { get; set; }
    public string IconLink { get; set; }
    public int BuyPrice { get; set; }
    public int Coins { get; set; }
    public int ScrapAmount { get; set; }
    public int LevelRequirement { get; set; }
    public string AuthorCredits { get; set; }

    public GunCharm(ushort charmID, string charmName, string charmDesc, ERarity charmRarity, string iconLink, int buyPrice, int coins, int scrapAmount, int levelRequirement, string authorCredits)
    {
        CharmID = charmID;
        CharmName = charmName;
        CharmDesc = charmDesc;
        CharmRarity = charmRarity;
        IconLink = iconLink;
        BuyPrice = buyPrice;
        Coins = coins;
        ScrapAmount = scrapAmount;
        LevelRequirement = levelRequirement;
        AuthorCredits = authorCredits;
    }

    public int GetCoins(int currentLevel)
    {
        var levelsNeeded = LevelRequirement - currentLevel;
        var coinsRequired = Coins * levelsNeeded;
        return coinsRequired > 0 ? coinsRequired : 0;
    }
}