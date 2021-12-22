using Rocket.Unturned;
using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.GameTypes;
using UnturnedLegends.Models;
using UnturnedLegends.Structs;

namespace UnturnedLegends.Managers
{
    public class GameManager
    {
        public Dictionary<CSteamID, GamePlayer> Players { get; set; }
        public Game CurrentGame { get; set; }

        public GameManager()
        {
            Players = new Dictionary<CSteamID, GamePlayer>();

            U.Events.OnPlayerConnected += OnPlayerJoined;
            U.Events.OnPlayerDisconnected += OnPlayerLeft;
        }

        private void OnPlayerJoined(UnturnedPlayer player)
        {
            Utility.Debug($"{player.CharacterName} joined the server, creating a game player and sending them to lobby!");
            if (!Players.ContainsKey(player.CSteamID))
            {
                Players.Add(player.CSteamID, new GamePlayer(player, player.Player.channel.GetOwnerTransportConnection()));
            }
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

        public void Destroy()
        {
            U.Events.OnPlayerConnected -= OnPlayerJoined;
            U.Events.OnPlayerDisconnected -= OnPlayerLeft;
        }
    }
}
