using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Database.Base;

public class Ability
{
    public int AbilityID { get; set; }
    public string AbilityName { get; set; }
    public string AbilityDesc { get; set; }
    public ERarity AbilityRarity { get; set; }
    public string IconLink { get; set; }
    public int BuyPrice { get; set; }
    public int Coins { get; set; }
    public int ScrapAmount { get; set; }
    public int LevelRequirement { get; set; }
    public AbilityData AbilityInfo { get; set; }

    public Ability(int abilityID, string abilityName, string abilityDesc, ERarity abilityRarity, string iconLink, int buyPrice, int coins, int scrapAmount, int levelRequirement, AbilityData abilityInfo)
    {
        AbilityID = abilityID;
        AbilityName = abilityName;
        AbilityDesc = abilityDesc;
        AbilityRarity = abilityRarity;
        IconLink = iconLink;
        BuyPrice = buyPrice;
        Coins = coins;
        ScrapAmount = scrapAmount;
        LevelRequirement = levelRequirement;
        AbilityInfo = abilityInfo;
    }

    public int GetCoins(int currentLevel) => Coins;
}