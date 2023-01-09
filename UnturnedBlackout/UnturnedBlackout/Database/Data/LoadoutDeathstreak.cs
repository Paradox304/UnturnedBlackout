using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutDeathstreak
{
    public Deathstreak Deathstreak { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutDeathstreak(Deathstreak deathstreak, bool isBought, bool isUnlocked)
    {
        Deathstreak = deathstreak;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }
}