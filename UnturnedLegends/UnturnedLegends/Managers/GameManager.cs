using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnturnedLegends.Enums;
using UnturnedLegends.GameTypes;
using UnturnedLegends.Models;
using UnturnedLegends.Structs;

namespace UnturnedLegends.Managers
{
    public class GameManager
    {
        public Config Config { get; set; }

        public Dictionary<CSteamID, GamePlayer> Players { get; set; }
        public Game CurrentGame { get; set; }

        public ArenaLocation PreviousLocation { get; set; }
        public EGameType PreviousGame { get; set; }

        public GameManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
            Players = new Dictionary<CSteamID, GamePlayer>();

            PreviousLocation = new ArenaLocation(-1, "None");
            PreviousGame = EGameType.None;

            U.Events.OnPlayerConnected += OnPlayerJoined;
            U.Events.OnPlayerDisconnected += OnPlayerLeft;

            StartGame();
        }

        public void StartGame()
        {
            Utility.Debug($"Starting game, finding a random arena location, previous location was {PreviousLocation.LocationName}");
            var locations = Config.ArenaLocations.Where(k => k.LocationID != PreviousLocation.LocationID).ToList();
            var randomLocation = locations[UnityEngine.Random.Range(0, locations.Count)];
            Utility.Debug($"Found a random location, name is {randomLocation.LocationName}");

            PreviousLocation = randomLocation;
            PreviousGame = EGameType.FFA;

            CurrentGame = new FFAGame(randomLocation.LocationID);
            Utility.Debug($"Started a {PreviousGame} game");
        }

        public void EndGame()
        {
            Utility.Debug($"Ending the game, destroying the Current Game and starting the timer to start another game");
            CurrentGame.Destroy();
            CurrentGame = null;
            StartGame();
        }

        public void AddPlayerToGame(UnturnedPlayer player)
        {
            Utility.Debug($"Trying to add {player.CharacterName} to game");
            if (CurrentGame == null)
            {
                Utility.Debug("There's no game going on at the moment");
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                Utility.Debug("Error finding game player of the player, returning");
                return;
            }

            CurrentGame.AddPlayerToGame(gPlayer);
        }

        public void RemovePlayerFromGame(UnturnedPlayer player)
        {
            Utility.Debug($"Trying to remove {player.CharacterName} from game");
            if (CurrentGame == null)
            {
                Utility.Debug("There's no game going on at the moment");
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                Utility.Debug("Error finding game player of the player, returning");
                return;
            }

            CurrentGame.RemovePlayerFromGame(gPlayer);
            Utility.Debug("Sending player to lobby");
            SendPlayerToLobby(player);
        }

        private void OnPlayerJoined(UnturnedPlayer player)
        {
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                var profile = player.SteamProfile;
                await Plugin.Instance.DBManager.AddOrUpdatePlayerAsync(player.CSteamID, profile.SteamID, profile.AvatarFull.ToString());
                await Plugin.Instance.DBManager.GetPlayerDataAsync(player.CSteamID);
            });

            Utility.Debug($"{player.CharacterName} joined the server, creating a game player and sending them to lobby!");
            if (!Players.ContainsKey(player.CSteamID))
            {
                Players.Remove(player.CSteamID);   
            }

            Players.Add(player.CSteamID, new GamePlayer(player, player.Player.channel.GetOwnerTransportConnection()));
            SendPlayerToLobby(player);
        }

        private void OnPlayerLeft(UnturnedPlayer player)
        {
            Utility.Debug($"{player.CharacterName} left the server, removing them from game and removing the game player");
            if (Players.TryGetValue(player.CSteamID, out GamePlayer gPlayer))
            {
                if (CurrentGame != null)
                    CurrentGame.RemovePlayerFromGame(gPlayer);
                Players.Remove(player.CSteamID);
            }
        }

        public void SendPlayerToLobby(UnturnedPlayer player)
        {
            Utility.Debug($"Sending {player.CharacterName} to the lobby");
            player.Player.inventory.ClearInventory();
            TaskDispatcher.QueueOnMainThread(() =>
            {
                player.Player.teleportToLocationUnsafe(Config.LobbySpawn, 0);
            });
        }

        public void Destroy()
        {
            U.Events.OnPlayerConnected -= OnPlayerJoined;
            U.Events.OnPlayerDisconnected -= OnPlayerLeft;
        }

        public GamePlayer GetGamePlayer(UnturnedPlayer player)
        {
            return Players.TryGetValue(player.CSteamID, out GamePlayer gPlayer) ? gPlayer : null;
        }
    }
}
