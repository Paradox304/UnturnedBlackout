using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutDeathstreak
{
    public Deathstreak Deathstreak { get; set; }
    public int DeathstreakKills { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutDeathstreak(Deathstreak deathstreak, int deathstreakKills, bool isBought, bool isUnlocked)
    {
        Deathstreak = deathstreak;
        DeathstreakKills = deathstreakKills;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }
}