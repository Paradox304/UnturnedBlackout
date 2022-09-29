namespace UnturnedBlackout.Database.Base;

public class XPLevel
{
    public int Level { get; set; }
    public int XPNeeded { get; set; }
    public string IconLinkLarge { get; set; }
    public string IconLinkMedium { get; set; }
    public string IconLinkSmall { get; set; }

    public XPLevel(int level, int xPNeeded, string iconLinkLarge, string iconLinkMedium, string iconLinkSmall)
    {
        Level = level;
        XPNeeded = xPNeeded;
        IconLinkLarge = iconLinkLarge;
        IconLinkMedium = iconLinkMedium;
        IconLinkSmall = iconLinkSmall;
    }
}