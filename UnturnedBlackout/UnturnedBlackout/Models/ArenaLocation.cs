namespace UnturnedBlackout.Models
{
    public class ArenaLocation
    {
        public int LocationID { get; set; }
        public int MaxPlayers { get; set; }
        public string LocationName { get; set; }
        public string ImageLink { get; set; }
        public int NavMesh { get; set; }
        public int BlueTeamID { get; set; }
        public int RedTeamID { get; set; }

        public ArenaLocation()
        {

        }

        public ArenaLocation(int locationID, int maxPlayers, string locationName, string imageLink, int navMesh, int blueTeamID, int redTeamID)
        {
            LocationID = locationID;
            MaxPlayers = maxPlayers;
            LocationName = locationName;
            ImageLink = imageLink;
            NavMesh = navMesh;
            BlueTeamID = blueTeamID;
            RedTeamID = redTeamID;
        }
    }
}
