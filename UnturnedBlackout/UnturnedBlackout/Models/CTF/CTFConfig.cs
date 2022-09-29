namespace UnturnedBlackout.Models.CTF;

public class CTFConfig
{
    public int StartSeconds { get; set; }
    public int EndSeconds { get; set; }
    public float WinMultiplier { get; set; }

    public int ScoreLimit { get; set; }
    public int SpawnProtectionSeconds { get; set; }
    public int RespawnSeconds { get; set; }

    public ushort RedFlagID { get; set; }
    public ushort BlueFlagID { get; set; }

    public float FlagCarryingSpeed { get; set; }

    public CTFConfig()
    {
        StartSeconds = 10;
        EndSeconds = 600;
        WinMultiplier = 0.5f;
        ScoreLimit = 3;
        SpawnProtectionSeconds = 2;
        RespawnSeconds = 5;
        RedFlagID = 26831;
        BlueFlagID = 26830;
        FlagCarryingSpeed = -0.25f;
    }
}