using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Models.Global;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout.Managers;

public class GameManager
{
    public ConfigManager Config => Plugin.Instance.Config;

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
        Logging.Debug("Starting games");
        for (var i = 1; i <= Config.Base.FileData.GamesCount; i++)
        {
            Logging.Debug($"{AvailableLocations.Count} locations available");
            var locationID = AvailableLocations[UnityEngine.Random.Range(0, AvailableLocations.Count)];
            var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
            var gameMode = GetRandomGameMode(locationID);
            Logging.Debug($"Found Location: {location.LocationName}, GameMode: {gameMode.Item1}, IsHardcore: {gameMode.Item2}");
            StartGame(location, gameMode.Item1, gameMode.Item2);
        }
    }

    public void StartGame(ArenaLocation location, EGameType gameMode, bool isHardcore)
    {
        Logging.Debug($"Starting game");
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

        Logging.Debug($"Game started, adding game to games and removing location from available locations");
        Games.Add(game);
        if (!AvailableLocations.Contains(location.LocationID))
        {
            Logging.Debug($"LOCATION {location.LocationName} IS NOT FOUND IN THE AVAILABLE LOCATIONS, WHAT TO REMOVE????");
        }
        else
        {
            _ = AvailableLocations.Remove(location.LocationID);
        }

        Plugin.Instance.UI.OnGameUpdated();
    }

    public void EndGame(Game game)
    {
        Logging.Debug($"Ending game with location {game.Location.LocationName}");
        game.Destroy();
        Logging.Debug($"Removing game and adding locations to available locations");
        _ = Games.Remove(game);
        if (AvailableLocations.Contains(game.Location.LocationID))
        {
            Logging.Debug($"LOCATION {game.Location.LocationName} IS ALREADY AVAILABLE IN THE AVAILABLELOCATIONS LIST, ERRORRRRR");
        }
        else
        {
            AvailableLocations.Add(game.Location.LocationID);
        }
    }

    public void AddPlayerToGame(UnturnedPlayer player, int selectedID)
    {
        if (selectedID > (Games.Count - 1))
        {
            Utility.Say(player, Plugin.Instance.Translate("Game_Not_Found_With_ID").ToRich());
            return;
        }

        var gPlayer = GetGamePlayer(player);
        if (gPlayer == null)
        {
            return;
        }

        if (gPlayer.CurrentGame != null)
        {
            Utility.Say(player, Plugin.Instance.Translate("Ingame").ToRich());
            return;
        }

        var game = Games[selectedID];
        if (game.GetPlayerCount() >= game.Location.GetMaxPlayers(game.GameMode))
        {
            Utility.Say(player, Plugin.Instance.Translate("Game_Full").ToRich());
            return;
        }

        Plugin.Instance.UI.HideMenuUI(gPlayer.Player);
        _ = Plugin.Instance.StartCoroutine(game.AddPlayerToGame(gPlayer));
    }

    public void RemovePlayerFromGame(UnturnedPlayer player)
    {
        var gPlayer = GetGamePlayer(player);
        if (gPlayer == null)
        {
            return;
        }

        if (gPlayer.CurrentGame == null)
        {
            Utility.Say(player, Plugin.Instance.Translate("Not_Ingame").ToRich());
            return;
        }

        gPlayer.CurrentGame.RemovePlayerFromGame(gPlayer);
        SendPlayerToLobby(player);
    }

    private void OnPlayerJoined(UnturnedPlayer player)
    {
        SendPlayerToLobby(player);

        var db = Plugin.Instance.DB;
        Plugin.Instance.UI.SendLoadingUI(player, false, EGameType.None, null, "LOADING...");
        _ = Task.Run(async () =>
        {
            var avatarURL = "";
            var countryCode = "EU";
            try
            {
                avatarURL = player.SteamProfile.AvatarFull.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting the steam profile for the player");
                Logger.Log(ex);
            }

            try
            {
                using HttpClient wc = new();
                countryCode = await wc.GetStringAsync($"http://ipinfo.io/{player.IP}/country");
                countryCode = countryCode.Replace("\n", "").Replace("\r", "");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting the country code for the player with ip {player.IP}");
                Logger.Log(ex);
            }
            finally
            {
                Logging.Debug($"{player.CharacterName}, country code: {countryCode}");
                await db.AddPlayerAsync(player, player.CharacterName, avatarURL, countryCode);
                await db.GetPlayerDataAsync(player);
            }

            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UI.ClearLoadingUI(player);
                player.Player.quests.leaveGroup(true);
                if (Players.ContainsKey(player.CSteamID))
                {
                    _ = Players.Remove(player.CSteamID);
                }

                Players.Add(player.CSteamID, new GamePlayer(player, player.Player.channel.GetOwnerTransportConnection()));
                Plugin.Instance.UI.RegisterUIHandler(player);
            });
        });
    }

    private void OnPlayerLeft(UnturnedPlayer player)
    {
        Plugin.Instance.UI.UnregisterUIHandler(player);
        if (Players.TryGetValue(player.CSteamID, out var gPlayer))
        {
            var game = gPlayer.CurrentGame;
            if (game != null)
            {
                game.RemovePlayerFromGame(gPlayer);
            }

            _ = Players.Remove(player.CSteamID);
        }
    }

    private void OnPlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
    {
        var ply = UnturnedPlayer.FromPlayer(sender.player);
        if (ply == null)
        {
            return;
        }

        if (Players.TryGetValue(ply.CSteamID, out var gPlayer))
        {
            var game = gPlayer.CurrentGame;
            if (game == null)
            {
                SendPlayerToLobby(ply);
            }
        }
    }

    private void OnMessageSent(SteamPlayer player, EChatMode mode, ref UnityEngine.Color chatted, ref bool isRich, string text, ref bool isVisible)
    {
        if (Players.TryGetValue(player.playerID.steamID, out var gPlayer))
        {
            var game = gPlayer.CurrentGame;
            if (game == null)
            {
                isVisible = false;
            }
        }
    }

    public void SendPlayerToLobby(UnturnedPlayer player, MatchEndSummary summary = null)
    {
        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        player.Player.inventory.ClearInventory();
        player.Player.life.serverModifyHealth(100);
        TaskDispatcher.QueueOnMainThread(() =>
        {
            player.Player.life.ServerRespawn(false);
            player.Player.teleportToLocationUnsafe(Config.Base.FileData.LobbySpawn, Config.Base.FileData.LobbyYaw);
            Plugin.Instance.UI.ShowMenuUI(player, summary);
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
        var poolSize = 0;
        foreach (var option in options) poolSize += option.GamemodeWeight;
        var randInt = UnityEngine.Random.Range(0, poolSize) + 1;

        var accumulatedProbability = 0;
        for (var i = 0; i < options.Count; i++)
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

    public GamePlayer GetGamePlayer(UnturnedPlayer player) => Players.TryGetValue(player.CSteamID, out var gPlayer) ? gPlayer : null;

    public GamePlayer GetGamePlayer(CSteamID steamID) => Players.TryGetValue(steamID, out var gPlayer) ? gPlayer : null;

    public GamePlayer GetGamePlayer(Player player) => Players.TryGetValue(player.channel.owner.playerID.steamID, out var gPlayer) ? gPlayer : null;

    /*
    public bool TryGetCurrentGame(CSteamID steamID, out Game game)
    {
        game = Games.FirstOrDefault(k => k.IsPlayerIngame(steamID));
        return game != null;
    }
    */
}
