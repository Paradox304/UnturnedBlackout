using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutPerk
    {
        public Perk Perk { get; set; }
        public bool IsBought { get; set; }

        public LoadoutPerk(Perk perk, bool isBought)
        {
            Perk = perk;
            IsBought = isBought;
        }
    }
}
