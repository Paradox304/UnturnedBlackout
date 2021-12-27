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
        public List<Game> Games { get; set; }

        public List<int> AvailableLocations { get; set; }

        public GameManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
            Players = new Dictionary<CSteamID, GamePlayer>();
            Games = new List<Game>();
            AvailableLocations = Config.ArenaLocations.Select(k => k.LocationID).ToList();

            U.Events.OnPlayerConnected += OnPlayerJoined;
            U.Events.OnPlayerDisconnected += OnPlayerLeft;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevived;

            DamageTool.damagePlayerRequested += OnDamagePlayer;

            StartGames();
        }

        public void StartGames()
        {
            Utility.Debug($"Starting games");
            foreach (var arena in Config.Arenas)
            {
                Utility.Debug($"Starting ")
                var locations = arena.Locations.Where(k => AvailableLocations.Contains(k)).ToList();
            }
        }

        public void EndGame(Game game)
        {
            // TO BE ADDED
        }

        public void AddPlayerToGame(UnturnedPlayer player)
        {
            // TO BE ADDED
        }

        public void RemovePlayerFromGame(UnturnedPlayer player)
        {
            // TO BE ADDED
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
            // TO BE ADDED
        }

        private void OnPlayerRevived(UnturnedPlayer player, Vector3 position, byte angle)
        {
            // TO BE ADDED
        }

        private void OnDamagePlayer(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            // TO BE ADDED
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
