using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutKnife
{
    public Knife Knife { get; set; }
    public int KnifeKills { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutKnife(Knife knife, int knifeKills, bool isBought, bool isUnlocked)
    {
        Knife = knife;
        KnifeKills = knifeKills;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }
}
