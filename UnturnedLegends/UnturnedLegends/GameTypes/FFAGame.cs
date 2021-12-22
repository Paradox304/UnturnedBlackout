using Rocket.Core;
using Rocket.Core.Utils;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.Enums;
using UnturnedLegends.Models;
using UnturnedLegends.SpawnPoints;
using UnturnedLegends.Structs;

namespace UnturnedLegends.GameTypes
{
    public class FFAGame : Game
    {
        public List<FFASpawnPoint> SpawnPoints { get; set; }
        public HashSet<CSteamID> Players { get; set; }

        public FFAGame(int locationID) : base(EGameType.FFA, locationID)
        {
            Utility.Debug("Initializing FFA game");
            SpawnPoints = Plugin.Instance.DataManager.Data.FFASpawnPoints.Where(k => k.LocationID == locationID).ToList();
            Players = new HashSet<CSteamID>();
            Utility.Debug($"Found {SpawnPoints.Count} positions for FFA");
        }

        public override void AddPlayerToGame(GamePlayer player)
        {
            Utility.Debug($"Adding {player.Player.CharacterName} to FFA game");
            if (Players.Contains(player.SteamID))
            {
                Utility.Debug("Player is already in the game, returning");
                return;
            }

            Players.Add(player.SteamID);
            GiveLoadout(player);
            SpawnPlayer(player);
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            Utility.Debug($"Removing {player.Player.CharacterName} from FFA game");
            if (!Players.Contains(player.SteamID))
            {
                Utility.Debug("Player is already not in the game, returning");
                return;
            }

            Players.Remove(player.SteamID);
        }

        public override void OnPlayerDead(Player player, CSteamID killer)
        {
            Utility.Debug($"Player died, getting the game player");
            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                Utility.Debug("Could'nt find the game player, returning");
                return;
            }
            Utility.Debug($"Game player found, player name: {gPlayer.Player.CharacterName}");
            Utility.Debug("Respawning the player");
            player.life.sendRespawn(false);
            TaskDispatcher.QueueOnMainThread(() =>
            {
                SpawnPlayer(gPlayer);
            });
        }


        public void GiveLoadout(GamePlayer player)
        {
            Utility.Debug($"Giving loadout to {player.Player.CharacterName}");
            R.Commands.Execute(player.Player, $"/kit {Config.KitName}");
        }

        public void SpawnPlayer(GamePlayer player)
        {
            Utility.Debug($"Spawning {player.Player.CharacterName}, getting a random location");
            if (SpawnPoints.Count == 0)
            {
                Utility.Debug("No spawnpoints set for FFA, returning");
                return;
            } 

            var spawnpoint = SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)];
            player.Player.Player.teleportToLocationUnsafe(spawnpoint.GetSpawnPoint(), 0);
        }
    }
}
