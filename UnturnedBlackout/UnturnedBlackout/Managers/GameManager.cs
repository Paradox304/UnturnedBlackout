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
using UnturnedBlackout.Enums;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Models;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout.Managers
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

            StartGames();
        }

        public void StartGames()
        {
            Utility.Debug($"Starting games");
            for (int i = 1; i <= Config.GamesCount; i++)
            {
                Utility.Debug($"Getting the location and setting the gamemode default to FFA for game {i}");
                Utility.Debug($"{AvailableLocations.Count} locations to choose from");
                var locationID = AvailableLocations[UnityEngine.Random.Range(0, AvailableLocations.Count)];
                var location = Config.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
                Utility.Debug($"Found {location.LocationName}");
                StartGame(location, EGameType.TDM);
            }
        }

        public void StartGame(ArenaLocation location, EGameType gameMode)
        {
            Utility.Debug($"Starting game with location {location.LocationID} for gamemode {gameMode}");
            Game game = null;
            switch (gameMode)
            {
                case EGameType.FFA:
                    game = new FFAGame(location);
                    break;
                case EGameType.TDM:
                    game = new TDMGame(location);
                    break;
                default:
                    break;
            }
            Utility.Debug("Game is created, adding the game to the list, and releasing the location from available locations");
            Games.Add(game);
            AvailableLocations.Remove(location.LocationID);
            Plugin.Instance.UIManager.OnGamesUpdated();
        }

        public void EndGame(Game game)
        {
            Utility.Debug($"Ending game for location {game.Location.LocationName} for gamemode {game.GameMode}");
            Utility.Debug("Destroying the game, removing the game from the list, and adding the location to available locations");
            game.Destroy();
            Games.Remove(game);
            AvailableLocations.Add(game.Location.LocationID);
        }

        public void AddPlayerToGame(UnturnedPlayer player, int selectedID)
        {
            Utility.Debug($"Trying to add {player.CharacterName} to game with id {selectedID}");
            if (selectedID > (Games.Count - 1))
            {
                Utility.Say(player, Plugin.Instance.Translate("Game_Not_Found_With_ID").ToRich());
                return;
            }
            var game = Games[selectedID];

            if (TryGetCurrentGame(player.CSteamID, out _))
            {
                Utility.Say(player, Plugin.Instance.Translate("Ingame").ToRich());
                return;
            }

            if (game.GetPlayerCount() >= game.Location.MaxPlayers)
            {
                Utility.Say(player, Plugin.Instance.Translate("Game_Full").ToRich());
                return;
            }

            if (game.GamePhase == EGamePhase.Voting || game.GamePhase == EGamePhase.WaitingForVoting)
            {
                Utility.Say(player, Plugin.Instance.Translate("Game_Voting").ToRich());
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                Utility.Debug("Error finding game player of the player, returning");
                return;
            }

            Plugin.Instance.UIManager.HideMenuUI(gPlayer.Player);
            game.AddPlayerToGame(gPlayer);
        }

        public void RemovePlayerFromGame(UnturnedPlayer player)
        {
            Utility.Debug($"Trying to remove {player.CharacterName} from game");
            if (!TryGetCurrentGame(player.CSteamID, out Game game))
            {
                Utility.Say(player, Plugin.Instance.Translate("Not_Ingame").ToRich());
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                Utility.Debug("Error finding game player of the player, returning");
                return;
            }

            game.RemovePlayerFromGame(gPlayer);
            Utility.Debug("Sending player to lobby");
            SendPlayerToLobby(player);
        }

        private void OnPlayerJoined(UnturnedPlayer player)
        {
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                var avatarURL = "";
                try
                {
                    avatarURL = player.SteamProfile.AvatarFull.ToString();
                } catch (Exception ex)
                {
                    Utility.Debug("Error getting the steam profile for the player");
                    Logger.Log(ex);
                } finally
                {
                    await Plugin.Instance.DBManager.AddOrUpdatePlayerAsync(player.CSteamID, player.CharacterName.ToUnrich(), avatarURL);
                    await Plugin.Instance.DBManager.GetPlayerDataAsync(player.CSteamID);
                }

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    Plugin.Instance.UIManager.RegisterUIHandler(player);
                    Plugin.Instance.HUDManager.OnXPChanged(player);

                    Utility.Debug($"{player.CharacterName} joined the server, creating a game player and sending them to lobby!");
                    if (!Players.ContainsKey(player.CSteamID))
                    {
                        Players.Remove(player.CSteamID);
                    }

                    Players.Add(player.CSteamID, new GamePlayer(player, player.Player.channel.GetOwnerTransportConnection()));
                    SendPlayerToLobby(player);
                });
            });
        }

        private void OnPlayerLeft(UnturnedPlayer player)
        {
            Utility.Debug($"{player.CharacterName} left the server, removing them from game and removing the game player");
            Plugin.Instance.UIManager.UnregisterUIHandler(player);
            if (Players.TryGetValue(player.CSteamID, out GamePlayer gPlayer))
            {
                if (TryGetCurrentGame(player.CSteamID, out Game game))
                {
                    game.RemovePlayerFromGame(gPlayer);
                }

                Players.Remove(player.CSteamID);
            }
        }

        public void OnPlayerVoted(UnturnedPlayer player, int index, int choice)
        {
            Utility.Debug($"{player.CSteamID} voted on {index} with choice {choice}");
            if (choice > (Games.Count - 1))
            {
                Utility.Debug("Game wasn't found, returning");
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                Utility.Debug("Error finding game player of the unturned player, returning");
                return;
            }

            var game = Games[index];
            if (game.GamePhase == EGamePhase.Voting)
            {
                game.OnVoted(gPlayer, choice);
            }
        }

        private void OnPlayerRevived(UnturnedPlayer player, Vector3 position, byte angle)
        {
            Utility.Debug("Player revived, checking if the player is in a game");
            if (!TryGetCurrentGame(player.CSteamID, out _))
            {
                Utility.Debug("Player is not in a game, spawning them in the lobby");
                SendPlayerToLobby(player);
            }
        }

        public void OnVotingEnded()
        {
            Utility.Debug("Voting ended for a map, checking if some other game is waiting to vote and start that");
            foreach (var game in Games)
            {
                if (game.GamePhase == EGamePhase.WaitingForVoting)
                {
                    game.StartVoting();
                    break;
                }
            }
        }

        public bool CanStartVoting()
        {
            foreach (var game in Games)
            {
                if (game.GamePhase == EGamePhase.Voting)
                {
                    return false;
                }
            }
            return true;
        }

        public void SendPlayerToLobby(UnturnedPlayer player)
        {
            Utility.Debug($"Sending {player.CharacterName} to the lobby");
            player.Player.inventory.ClearInventory();
            player.Player.life.serverModifyHealth(100);
            TaskDispatcher.QueueOnMainThread(() =>
            {
                player.Player.life.ServerRespawn(false);
                player.Player.teleportToLocationUnsafe(Config.LobbySpawn, 0);
                Plugin.Instance.UIManager.ShowMenuUI(player);
            });
        }

        public void Destroy()
        {
            U.Events.OnPlayerConnected -= OnPlayerJoined;
            U.Events.OnPlayerDisconnected -= OnPlayerLeft;

            UnturnedPlayerEvents.OnPlayerRevive -= OnPlayerRevived;
        }

        public GamePlayer GetGamePlayer(UnturnedPlayer player)
        {
            return Players.TryGetValue(player.CSteamID, out GamePlayer gPlayer) ? gPlayer : null;
        }

        public GamePlayer GetGamePlayer(CSteamID steamID)
        {
            return Players.TryGetValue(steamID, out GamePlayer gPlayer) ? gPlayer : null;
        }

        public GamePlayer GetGamePlayer(Player player)
        {
            return Players.TryGetValue(player.channel.owner.playerID.steamID, out GamePlayer gPlayer) ? gPlayer : null;
        }

        public bool TryGetCurrentGame(CSteamID steamID, out Game game)
        {
            game = Games.FirstOrDefault(k => k.IsPlayerIngame(steamID));
            return game != null;
        }
    }
}
