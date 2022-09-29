using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutGlove
{
    public Glove Glove { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutGlove(Glove glove, bool isBought, bool isUnlocked)
    {
        Glove = glove;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }
}