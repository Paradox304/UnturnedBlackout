﻿using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnturnedBlackout.Enums;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Models.Global;
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
            PlayerLife.onPlayerDied += OnPlayerDeath;

            StartGames();
        }

        public void StartGames()
        {
            var gameModes = new List<byte> { (byte)EGameType.FFA, (byte)EGameType.TDM, (byte)EGameType.KC, (byte)EGameType.CTF };

            for (int i = 1; i <= Config.GamesCount; i++)
            {
                Utility.Debug($"Getting the location");
                Utility.Debug($"{AvailableLocations.Count} locations to choose from");
                var locationID = AvailableLocations[UnityEngine.Random.Range(0, AvailableLocations.Count)];
                var location = Config.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
                var gameMode = (EGameType)gameModes[UnityEngine.Random.Range(0, gameModes.Count)];
                Utility.Debug($"Found {gameMode}");
                Utility.Debug($"Found {location.LocationName}");
                StartGame(location, gameMode);
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
                case EGameType.KC:
                    game = new KCGame(location);
                    break;
                case EGameType.CTF:
                    game = new CTFGame(location);
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

            if (game.GetPlayerCount() >= game.Location.GetMaxPlayers(game.GameMode))
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
                return;
            }

            Plugin.Instance.UIManager.HideMenuUI(gPlayer.Player);
            Plugin.Instance.StartCoroutine(game.AddPlayerToGame(gPlayer));
        }

        public void RemovePlayerFromGame(UnturnedPlayer player)
        {
            if (!TryGetCurrentGame(player.CSteamID, out Game game))
            {
                Utility.Say(player, Plugin.Instance.Translate("Not_Ingame").ToRich());
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                return;
            }

            game.RemovePlayerFromGame(gPlayer);
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
                }
                catch (Exception ex)
                {
                    Logger.Log("Error getting the steam profile for the player");
                    Logger.Log(ex);
                }
                finally
                {
                    await Plugin.Instance.DBManager.AddOrUpdatePlayerAsync(player.CSteamID, player.CharacterName.ToUnrich(), avatarURL);
                    await Plugin.Instance.DBManager.GetPlayerDataAsync(player.CSteamID);
                }

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    Plugin.Instance.UIManager.RegisterUIHandler(player);
                    Plugin.Instance.HUDManager.OnXPChanged(player);

                    player.Player.quests.leaveGroup(true);
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
            if (index > (Games.Count - 1))
            {
                return;
            }

            var gPlayer = GetGamePlayer(player);
            if (gPlayer == null)
            {
                return;
            }

            var game = Games[index];
            if (game.GamePhase == EGamePhase.Voting)
            {
                game.OnVoted(gPlayer, choice);
            }
        }


        private void OnPlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (!TryGetCurrentGame(sender.player.channel.owner.playerID.steamID, out _))
            {
                SendPlayerToLobby(UnturnedPlayer.FromPlayer(sender.player));
            }
        }

        public void OnVotingEnded()
        {
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

            PlayerLife.onPlayerDied -= OnPlayerDeath;
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
