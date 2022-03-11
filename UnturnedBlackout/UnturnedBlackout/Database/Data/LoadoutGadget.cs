using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutGadget
    {
        public Gadget Gadget { get; set; }
        public int GadgetKills { get; set; }
        public bool IsBought { get; set; }

        public LoadoutGadget(Gadget gadget, int gadgetKills, bool isBought)
        {
            Gadget = gadget;
            GadgetKills = gadgetKills;
            IsBought = isBought;
        }
    }
}
