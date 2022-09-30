using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global;

public class ArenaLocation
{
    public int LocationID { get; set; }
    public string LocationName { get; set; }
    public string ImageLink { get; set; }
    public int NavMesh { get; set; }
    public int BlueTeamID { get; set; }
    public int RedTeamID { get; set; }

    public int FFACount { get; set; }
    public int TDMCount { get; set; }
    public int KCCount { get; set; }
    public int CTFCount { get; set; }

    public int FFAMinCount { get; set; }
    public int TDMMinCount { get; set; }
    public int KCMinCount { get; set; }
    public int CTFMinCount { get; set; }

    public float PositionCheck { get; set; }

    public ArenaLocation()
    {
    }

    public ArenaLocation(int locationID, string locationName, string imageLink, int navMesh, int blueTeamID, int redTeamID, int fFaCount, int tDmCount, int kCCount, int cTfCount, int fFaMinCount, int tDmMinCount, int kCMinCount, int cTfMinCount, float positionCheck)
    {
        LocationID = locationID;
        LocationName = locationName;
        ImageLink = imageLink;
        NavMesh = navMesh;
        BlueTeamID = blueTeamID;
        RedTeamID = redTeamID;
        FFACount = fFaCount;
        TDMCount = tDmCount;
        KCCount = kCCount;
        CTFCount = cTfCount;
        FFAMinCount = fFaMinCount;
        TDMMinCount = tDmMinCount;
        KCMinCount = kCMinCount;
        CTFMinCount = cTfMinCount;
        PositionCheck = positionCheck;
    }

    public int GetMaxPlayers(EGameType type)
    {
        return type switch
        {
            EGameType.FFA => FFACount,
            EGameType.TDM => TDMCount,
            EGameType.KC => KCCount,
            EGameType.CTF => CTFCount,
            var _ => 0
        };
    }

    public int GetMinPlayers(EGameType type)
    {
        return type switch
        {
            EGameType.FFA => FFAMinCount,
            EGameType.TDM => TDMMinCount,
            EGameType.KC => KCMinCount,
            EGameType.CTF => CTFMinCount,
            var _ => 0
        };
    }
}