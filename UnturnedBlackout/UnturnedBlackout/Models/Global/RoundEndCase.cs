using JetBrains.Annotations;

namespace UnturnedBlackout.Models.Global;

public class RoundEndCase
{
    public int CaseID { get; set; }
    public int Weight { get; set; }

    [UsedImplicitly]
    public RoundEndCase()
    {
    }
}