using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutCard
    {
        public Card Card { get; set; }
        public bool IsBought { get; set; }

        public LoadoutCard(Card card, bool isBought)
        {
            Card = card;
            IsBought = isBought;
        }
    }
}
