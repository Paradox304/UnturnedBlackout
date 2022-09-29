using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutGadget
{
    public Gadget Gadget { get; set; }
    public int GadgetKills { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutGadget(Gadget gadget, int gadgetKills, bool isBought, bool isUnlocked)
    {
        Gadget = gadget;
        GadgetKills = gadgetKills;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }
}