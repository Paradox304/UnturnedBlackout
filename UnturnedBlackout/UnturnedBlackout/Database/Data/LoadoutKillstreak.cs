using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutKillstreak
{
    public Killstreak Killstreak { get; set; }
    public int KillstreakKills { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutKillstreak(Killstreak killstreak, int killstreakKills, bool isBought, bool isUnlocked)
    {
        Killstreak = killstreak;
        KillstreakKills = killstreakKills;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }
}
