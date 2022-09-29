namespace UnturnedBlackout.Models.Global;

public class TeamGlove
{
    public int GloveID { get; set; }
    public ushort ItemID { get; set; }

    public TeamGlove()
    {
    }

    public TeamGlove(int gloveID, ushort itemID)
    {
        GloveID = gloveID;
        ItemID = itemID;
    }
}