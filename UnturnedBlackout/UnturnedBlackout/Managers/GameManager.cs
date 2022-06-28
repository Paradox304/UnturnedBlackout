using Rocket.Core.Utils;
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
        public ConfigManager Config
        {
            get
            {
                return Plugin.Instance.ConfigManager;
            }
        }

        public Dictionary<CSteamID, GamePlayer> Players { get; set; }
        public List<Game> Games { get; set; }

        public List<int> AvailableLocations { get; set; }

        public GameManager()
        {
            Players = new Dictionary<CSteamID, GamePlayer>();
            Games = new List<Game>();
            AvailableLocations = Config.Locations.FileData.ArenaLocations.Select(k => k.LocationID).ToList();

            U.Events.OnPlayerConnected += OnPlayerJoined;
            U.Events.OnPlayerDisconnected += OnPlayerLeft;
            PlayerLife.onPlayerDied += OnPlayerDeath;
            ChatManager.onChatted += OnMessageSent;

            StartGames();
        }

        public void StartGames()
        {
            for (int i = 1; i <= Config.Base.FileData.GamesCount; i++)
            {
                var locationID = AvailableLocations[UnityEngine.Random.Range(0, AvailableLocations.Count)];
                var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
                var gameMode = GetRandomGameMode(locationID);
                StartGame(location, gameMode.Item1, gameMode.Item2);
            }
        }

        public void StartGame(ArenaLocation location, EGameType gameMode, bool isHardcore)
        {
            Game game = null;
            switch (gameMode)
            {
                case EGameType.FFA:
                    game = new FFAGame(location, isHardcore);
                    break;
                case EGameType.TDM:
                    game = new TDMGame(location, isHardcore);
                    break;
                case EGameType.KC:
                    game = new KCGame(location, isHardcore);
                    break;
                case EGameType.CTF:
                    game = new CTFGame(location, isHardcore);
                    break;
                default:
                    break;
            }

            Games.Add(game);
            AvailableLocations.Remove(location.LocationID);
            Plugin.Instance.UIManager.OnGameUpdated();
        }

        public void EndGame(Game game)
        {
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
            SendPlayerToLobby(player);

            var db = Plugin.Instance.DBManager;
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
                    await db.AddPlayerAsync(player, player.CharacterName, avatarURL);
                    await db.GetPlayerDataAsync(player);
                }

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    player.Player.quests.leaveGroup(true);
                    if (Players.ContainsKey(player.CSteamID))
                    {
                        Players.Remove(player.CSteamID);
                    }

                    Players.Add(player.CSteamID, new GamePlayer(player, player.Player.channel.GetOwnerTransportConnection()));
                    Plugin.Instance.UIManager.RegisterUIHandler(player);
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

        private void OnPlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (!TryGetCurrentGame(sender.player.channel.owner.playerID.steamID, out _))
            {
                SendPlayerToLobby(UnturnedPlayer.FromPlayer(sender.player));
            }
        }

        private void OnMessageSent(SteamPlayer player, EChatMode mode, ref UnityEngine.Color chatted, ref bool isRich, string text, ref bool isVisible)
        {
            if (!Plugin.Instance.GameManager.TryGetCurrentGame(player.playerID.steamID, out _))
            {
                isVisible = false;
            }
        }

        public void SendPlayerToLobby(UnturnedPlayer player)
        {
            player.Player.inventory.ClearInventory();
            player.Player.life.serverModifyHealth(100);
            TaskDispatcher.QueueOnMainThread(() =>
            {
                player.Player.life.ServerRespawn(false);
                player.Player.teleportToLocationUnsafe(Config.Base.FileData.LobbySpawn, 0);
                Plugin.Instance.UIManager.ShowMenuUI(player);
            });
        }

        public (EGameType, bool) GetRandomGameMode(int locationID)
        {
            Logging.Debug($"Getting list of random gamemodes for location with id {locationID}");
            var gameModeOptions = Config.Gamemode.FileData.GamemodeOptions.Where(k => !k.IgnoredLocations.Contains(locationID)).ToList();
            Logging.Debug($"Found {gameModeOptions.Count} gamemode options to choose from");
            var option = CalculateRandomGameMode(gameModeOptions);
            Logging.Debug($"Found gamemode with name {option.GameType}");
            var isHardcore = option.HasHardcore && UnityEngine.Random.Range(0, 100) <= option.HardcoreChance;
            Logging.Debug($"hardcore: {isHardcore}");
            return (option.GameType, isHardcore);
        }

        public GamemodeOption CalculateRandomGameMode(List<GamemodeOption> options)
        {
            int poolSize = 0;
            foreach (var option in options) poolSize += option.GamemodeWeight;
            int randInt = UnityEngine.Random.Range(0, poolSize) + 1;

            int accumulatedProbability = 0;
            for (int i = 0; i < options.Count; i++)
            {
                accumulatedProbability += options[i].GamemodeWeight;
                if (randInt <= accumulatedProbability)
                    return options[i];
            }
            return options[UnityEngine.Random.Range(0, options.Count)];
        }

        public void Destroy()
        {
            U.Events.OnPlayerConnected -= OnPlayerJoined;
            U.Events.OnPlayerDisconnected -= OnPlayerLeft;

            PlayerLife.onPlayerDied -= OnPlayerDeath;

            ChatManager.onChatted -= OnMessageSent;
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
