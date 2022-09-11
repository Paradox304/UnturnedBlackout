using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutPerk
    {
        public Perk Perk { get; set; }
        public bool IsBought { get; set; }
        public bool IsUnlocked { get; set; }

        public LoadoutPerk(Perk perk, bool isBought, bool isUnlocked)
        {
            Perk = perk;
            IsBought = isBought;
            IsUnlocked = isUnlocked;
        }
    }
}
