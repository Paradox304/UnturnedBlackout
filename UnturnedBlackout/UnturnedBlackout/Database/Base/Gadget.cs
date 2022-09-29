using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base;

public class Gadget
{
    public ushort GadgetID { get; set; }
    public string GadgetName { get; set; }
    public string GadgetDesc { get; set; }
    public ERarity GadgetRarity { get; set; }
    public string IconLink { get; set; }
    public int Coins { get; set; }
    public int BuyPrice { get; set; }
    public int ScrapAmount { get; set; }
    public int GiveSeconds { get; set; }
    public int LevelRequirement { get; set; }
    public bool IsTactical { get; set; }

    public Gadget(
        ushort gadgetID,
        string gadgetName,
        string gadgetDesc,
        ERarity gadgetRarity,
        string iconLink,
        int coins,
        int buyPrice,
        int scrapAmount,
        int giveSeconds,
        int levelRequirement,
        bool isTactical)
    {
        GadgetID = gadgetID;
        GadgetName = gadgetName;
        GadgetDesc = gadgetDesc;
        GadgetRarity = gadgetRarity;
        IconLink = iconLink;
        Coins = coins;
        BuyPrice = buyPrice;
        ScrapAmount = scrapAmount;
        GiveSeconds = giveSeconds;
        LevelRequirement = levelRequirement;
        IsTactical = isTactical;
    }

    public int GetCoins(int currentLevel)
    {
        var levelsNeeded = LevelRequirement - currentLevel;
        var coinsRequired = Coins * levelsNeeded;
        return coinsRequired > 0 ? coinsRequired : 0;
    }
}