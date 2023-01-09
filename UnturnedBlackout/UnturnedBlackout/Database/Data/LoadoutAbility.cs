using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutAbility
{
    public Ability Ability { get; set; }
    public int AbilityKills { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutAbility(Ability ability, int abilityKills, bool isBought, bool isUnlocked)
    {
        Ability = ability;
        AbilityKills = abilityKills;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }
}