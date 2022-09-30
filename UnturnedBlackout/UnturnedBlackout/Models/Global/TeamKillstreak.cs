using JetBrains.Annotations;

namespace UnturnedBlackout.Models.Global;

public class TeamKillstreak
{
    public int KillstreakID { get; set; }
    public ushort ShirtID { get; set; }
    public ushort PantsID { get; set; }
    public ushort HatID { get; set; }
    public ushort VestID { get; set; }

    [UsedImplicitly]
    public TeamKillstreak()
    {
    }
}