using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
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
        public Game Game { get; set; }

        public ArenaLocation CurrentLocation { get; set; }
        public EGameType CurrentGame { get; set; }

        public GameManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
            Players = new Dictionary<CSteamID, GamePlayer>();

            CurrentLocation = new ArenaLocation(-1, "None");
            CurrentGame = EGameType.None;

            U.Events.OnPlayerConnected += OnPlayerJoined;
            U.Events.OnPlayerDisconnected += OnPlayerLeft;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevived;

            DamageTool.damagePlayerRequested += OnDamagePlayer;

            StartGame();
        }

        public void StartGame()
        {
            Utility.Debug($"Starting game, finding a random arena location, previous location was {CurrentLocation.LocationName}");
            var locations = Config.ArenaLocations.ToList();
            var randomLocation = locations[UnityEngine.Random.Range(0, locations.Count)];
            Utility.Debug($"Found a random location, name is {randomLocation.LocationName}");

            CurrentLocation = randomLocation;
            CurrentGame = EGameType.FFA;

            Game = new FFAGame(randomLocation.LocationID);
            foreach (var client in Provider.clients)
            {
                Plugin.Instance.HUDManager.OnGamemodeChanged(client.player, CurrentLocation, CurrentGame);
            }
            Utility.Debug($"Started a {CurrentGame} game");
        }

        public void EndGame()
        {
            Utility.Debug($"Ending the game, destroying the Current Game and starting the timer to start another game");
            Game.Destroy();
            Game = null;
            StartGame();
        }

        public void AddPlayerToGame(UnturnedPlayer player)
        {
            Utility.Debug($"Trying to add {player.CharacterName} to game");
            if (Game == null)
            {
                Utility.Say(player, Plugin.Instance.Translate("No_Game_Going_On").ToRich());
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                Utility.Debug("Error finding game player of the player, returning");
                return;
            }

            Game.AddPlayerToGame(gPlayer);
        }

        public void RemovePlayerFromGame(UnturnedPlayer player)
        {
            Utility.Debug($"Trying to remove {player.CharacterName} from game");
            if (Game == null)
            {
                Utility.Say(player, Plugin.Instance.Translate("No_Game_Going_On").ToRich());
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                Utility.Debug("Error finding game player of the player, returning");
                return;
            }

            Game.RemovePlayerFromGame(gPlayer);
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

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    Plugin.Instance.HUDManager.OnXPChanged(player);
                });
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
                if (Game != null)
                {
                    Game.RemovePlayerFromGame(gPlayer);
                }

                Players.Remove(player.CSteamID);
            }
        }

        private void OnPlayerRevived(UnturnedPlayer player, Vector3 position, byte angle)
        {
            Utility.Debug("Player revived, checking if the player is in a game");
            if (Game == null || !Game.IsPlayerIngame(player))
            {
                Utility.Debug("Player is not in a game, spawning them in the lobby");
                SendPlayerToLobby(player);
            }
        }

        private void OnDamagePlayer(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            Utility.Debug("Player damaged, checking if the player is in a game");
            if (Game == null || !Game.IsPlayerIngame(parameters.player))
            {
                Utility.Debug("Player is not in a game, disabling the damage done");
                shouldAllow = false;
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

            UnturnedPlayerEvents.OnPlayerRevive -= OnPlayerRevived;

            DamageTool.damagePlayerRequested -= OnDamagePlayer;
        }

        public GamePlayer GetGamePlayer(UnturnedPlayer player)
        {
            return Players.TryGetValue(player.CSteamID, out GamePlayer gPlayer) ? gPlayer : null;
        }
    }
}
