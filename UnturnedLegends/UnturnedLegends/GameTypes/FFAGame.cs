using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.Enums;
using UnturnedLegends.SpawnPoints;

namespace UnturnedLegends.GameTypes
{
    public class FFAGame : Game
    {
        public Config Config { get; set; }
        public List<FFASpawnPoint> SpawnPoints { get; set; }

        public FFAGame(int locationID) : base(EGameType.FFA)
        {
            Utility.Debug("Initializing FFA game");
            SpawnPoints = Plugin.Instance.DataManager.Data.FFASpawnPoints.Where(k => k.LocationID == locationID).ToList();
            Utility.Debug($"Found {SpawnPoints.Count} positions for FFA");


        }

        public override void OnPlayerDying(Player player)
        {
            
        }

        public override void OnPlayerDead(Player player, CSteamID killer)
        {
            
        }
    }
}
