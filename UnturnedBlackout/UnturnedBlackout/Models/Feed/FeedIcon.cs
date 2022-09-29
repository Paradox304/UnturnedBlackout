namespace UnturnedBlackout.Models.Feed;

public class FeedIcon
{
    public ushort WeaponID { get; set; }
    public string Symbol { get; set; }

    public FeedIcon(ushort weaponID, string symbol)
    {
        WeaponID = weaponID;
        Symbol = symbol;
    }

    public FeedIcon()
    {
    }
}