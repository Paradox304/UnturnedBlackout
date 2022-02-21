using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global
{
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

        public ArenaLocation()
        {

        }

        public ArenaLocation(int locationID, string locationName, string imageLink, int navMesh, int blueTeamID, int redTeamID, int fFACount, int tDMCount, int kCCount, int cTFCount, int fFAMinCount, int tDMMinCount, int kCMinCount, int cTFMinCount)
        {
            LocationID = locationID;
            LocationName = locationName;
            ImageLink = imageLink;
            NavMesh = navMesh;
            BlueTeamID = blueTeamID;
            RedTeamID = redTeamID;
            FFACount = fFACount;
            TDMCount = tDMCount;
            KCCount = kCCount;
            CTFCount = cTFCount;
            FFAMinCount = fFAMinCount;
            TDMMinCount = tDMMinCount;
            KCMinCount = kCMinCount;
            CTFMinCount = cTFMinCount;
        }

        public int GetMaxPlayers(EGameType type)
        {
            switch (type)
            {
                case EGameType.FFA:
                    return FFACount;
                case EGameType.TDM:
                    return TDMCount;
                case EGameType.KC:
                    return KCCount;
                case EGameType.CTF:
                    return CTFCount;
                default:
                    return 0;
            }
        }

        public int GetMinPlayers(EGameType type)
        {
            switch (type)
            {
                case EGameType.FFA:
                    return FFAMinCount;
                case EGameType.TDM:
                    return TDMMinCount;
                case EGameType.KC:
                    return KCMinCount;
                case EGameType.CTF:
                    return CTFMinCount;
                default:
                    return 0;
            }
        }
    }
}
