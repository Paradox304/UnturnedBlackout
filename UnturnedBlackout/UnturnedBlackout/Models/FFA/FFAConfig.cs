using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.FFA;

public class FFAConfig
{
    public int StartSeconds { get; set; }
    public int EndSeconds { get; set; }

    public TeamInfo FFATeam { get; set; }
    public float WinMultiplier { get; set; }

    public int ScoreLimit { get; set; }
    public int SpawnProtectionSeconds { get; set; }
    public int RespawnSeconds { get; set; }
}