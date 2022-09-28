using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutGunCharm
{
    public GunCharm GunCharm { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutGunCharm(GunCharm gunCharm, bool isBought, bool isUnlocked)
    {
        GunCharm = gunCharm;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }
}
