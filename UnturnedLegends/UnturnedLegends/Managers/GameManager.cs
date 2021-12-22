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
            Players = new Dictionary<CSteamID, GamePlayer>();

            U.Events.OnPlayerConnected += OnPlayerJoined;
            U.Events.OnPlayerDisconnected += OnPlayerLeft;
        }

        public void StartGame()
        {

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
                Players.Add(player.CSteamID, new GamePlayer(player, player.Player.channel.GetOwnerTransportConnection()));
            }
            SendPlayerToLobby(player);
        }

        private void OnPlayerLeft(UnturnedPlayer player)
        {
            Utility.Debug($"{player.CharacterName} left the server, removing them from game and removing the game player");
            if (Players.TryGetValue(player.CSteamID, out GamePlayer gPlayer))
            {
                CurrentGame.RemovePlayerFromGame(gPlayer);
                Players.Remove(player.CSteamID);
            }
        }

        public void SendPlayerToLobby(UnturnedPlayer player)
        {
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
    }
}
