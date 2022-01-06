namespace UnturnedBlackout.Models
{
    public class ArenaLocation
    {
        public int LocationID { get; set; }
        public int MaxPlayers { get; set; }
        public string LocationName { get; set; }
        public string ImageLink { get; set; }

        public ArenaLocation()
        {

        }

        public ArenaLocation(int locationID, int maxPlayers, string locationName, string imageLink)
        {
            LocationID = locationID;
            MaxPlayers = maxPlayers;
            LocationName = locationName;
            ImageLink = imageLink;
        }
    }
}
