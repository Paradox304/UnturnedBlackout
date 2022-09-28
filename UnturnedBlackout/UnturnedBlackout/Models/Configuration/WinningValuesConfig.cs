namespace UnturnedBlackout.Models.Configuration;

public class WinningValuesConfig
{
    public int PointsDivisible { get; set; }
    public int PointsPerMinutePlayed { get; set; }

    public int BonusXPVictoryDivisible { get; set; }
    public int BonusXPDefeatDivisible { get; set; }
    public int BonusXPPerMinutePlayed { get; set; }

    public int BPXPPerMinutePlayed { get; set; }
    public float BPXPVictoryBonus { get; set; }
    public float BPXPDefeatBonus { get; set; }

    public float PrimeXPBooster { get; set; }
    public float PrimeGunXPBooster { get; set; }
    public float PrimeBPXPBooster { get; set; }

    public WinningValuesConfig()
    {
        PointsDivisible = 25;
        PointsPerMinutePlayed = 10;

        BonusXPVictoryDivisible = 2;
        BonusXPDefeatDivisible = 3;
        BonusXPPerMinutePlayed = 100;

        BPXPPerMinutePlayed = 2;
        BPXPVictoryBonus = 0.3f;
        BPXPDefeatBonus = 0;

        PrimeXPBooster = 0.25f;
        PrimeGunXPBooster = 0.25f;
        PrimeBPXPBooster = 0;
    }
}
