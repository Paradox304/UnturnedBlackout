namespace UnturnedLegends.Models
{
    public class ArenaLocation
    {
        public int LocationID { get; set; }
        public int MaxPlayers { get; set; }
        public string LocationName { get; set; }

        public ArenaLocation()
        {

        }

        public ArenaLocation(int locationID, int maxPlayers, string locationName)
        {
            LocationID = locationID;
            MaxPlayers = maxPlayers;
            LocationName = locationName;
        }
    }
}
