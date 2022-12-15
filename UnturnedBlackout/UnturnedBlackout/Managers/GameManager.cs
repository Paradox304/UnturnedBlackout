using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Instances;
using UnturnedBlackout.Models.Global;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout.Managers;

public class GameManager
{
    public ConfigManager Config => Plugin.Instance.Config;

    public Dictionary<CSteamID, GamePlayer> Players { get; set; }
    public List<Game> Games { get; set; }

    public List<int> AvailableLocations { get; set; }

    public Coroutine GameChecker { get; set; }
    
    public GameManager()
    {
        Players = new();
        Games = new();
        AvailableLocations = Config.Locations.FileData.ArenaLocations.Select(k => k.LocationID).ToList();

        U.Events.OnPlayerConnected += OnPlayerJoined;
        U.Events.OnPlayerDisconnected += OnPlayerLeft;
        PlayerLife.onPlayerDied += OnPlayerDeath;
        ChatManager.onChatted += OnMessageSent;

        StartGames();
        GameChecker = Plugin.Instance.StartCoroutine(CheckGames());
    }

    private void StartGames()
    {
        for (var i = 1; i <= Config.Base.FileData.MinGamesCount; i++)
        {
            Logging.Debug($"{AvailableLocations.Count} locations available");
            var locationID = AvailableLocations[UnityEngine.Random.Range(0, AvailableLocations.Count)];
            var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
            if (location == null)
            {
                Logging.Debug($"Error finding a free location, returning");
                return;
            }

            var gameSetup = GetRandomGameSetup(location);
            Logging.Debug($"Found Location: {location.LocationName}, GameMode: {gameSetup.Item1}, Event: {gameSetup.Item2?.EventName ?? "None"}");
            StartGame(location, gameSetup.Item1, gameSetup.Item2, false);
        }
    }

    public void StartGame(ArenaLocation location, EGameType gameMode, GameEvent gameEvent, bool forceCheck = true)
    {
        Logging.Debug($"Starting game");
        if (forceCheck)
        {
            var gameCount = Games.Sum(k => k.GetPlayerCount());
            var gameMaxCount = Games.Sum(k => k.Location.GetMaxPlayers(k.GameMode));

            Logging.Debug($"Checking if allowed to start game, total games: {Games.Count}, players: {gameCount}, max count: {gameMaxCount}, threshold: {gameCount * 100 / gameMaxCount}, game threshold: {Config.Base.FileData.GameThreshold}");
            if (gameCount * 100 / gameMaxCount < Config.Base.FileData.GameThreshold && Games.Count >= Config.Base.FileData.MinGamesCount)
            {
                Logging.Debug($"Threshold below {Config.Base.FileData.GameThreshold}%, not starting game");
                Plugin.Instance.UI.OnGameUpdated();
                return;
            }
        }
        
        Game game = null;
        switch (gameMode)
        {
            case EGameType.FFA:
                game = new FFAGame(location, gameEvent);
                break;
            case EGameType.TDM:
                game = new TDMGame(location, gameEvent);
                break;
            case EGameType.KC:
                game = new KCGame(location, gameEvent);
                break;
            case EGameType.CTF:
                game = new CTFGame(location, gameEvent);
                break;
        }

        Logging.Debug($"Game started, adding game to games and removing location from available locations");
        Games.Add(game);
        if (!AvailableLocations.Contains(location.LocationID))
            Logging.Debug($"LOCATION {location.LocationName} IS NOT FOUND IN THE AVAILABLE LOCATIONS, WHAT TO REMOVE????");
        else
            _ = AvailableLocations.Remove(location.LocationID);

        Games = Games.OrderByDescending(k => k.GameEvent?.AlwaysHaveLobby ?? false).ToList();
        Plugin.Instance.UI.OnGameUpdated();
    }

    public void EndGame(Game game)
    {
        Logging.Debug($"Ending game with location {game.Location.LocationName}");
        Logging.Debug($"Removing game and adding locations to available locations");
        _ = Games.Remove(game);
        if (AvailableLocations.Contains(game.Location.LocationID))
            Logging.Debug($"LOCATION {game.Location.LocationName} IS ALREADY AVAILABLE IN THE AVAILABLELOCATIONS LIST, ERRORRRRR");
        else
            AvailableLocations.Add(game.Location.LocationID);
    }

    public void AddPlayerToGame(UnturnedPlayer player, int selectedID)
    {
        if (selectedID > Games.Count - 1)
        {
            Utility.Say(player, Plugin.Instance.Translate("Game_Not_Found_With_ID"));
            return;
        }

        var gPlayer = GetGamePlayer(player);
        if (gPlayer == null)
            return;

        if (gPlayer.CurrentGame != null)
        {
            Utility.Say(player, Plugin.Instance.Translate("Ingame"));
            return;
        }

        var game = Games[selectedID];
        if (game.GetPlayerCount() >= game.Location.GetMaxPlayers(game.GameMode))
        {
            Utility.Say(player, Plugin.Instance.Translate("Game_Full"));
            return;
        }

        if (game.GameEvent != null && game.GameEvent.MinLevel != 0 && gPlayer.Data.Level < game.GameEvent.MinLevel)
        {
            Utility.Say(player, $"[color=red]You need to be above Level {game.GameEvent.MinLevel} to join this lobby.[/color]");
            return;
        }

        if (game.GameEvent != null && game.GameEvent.MaxLevel != 0 && gPlayer.Data.Level > game.GameEvent.MaxLevel)
        {
            Utility.Say(player, $"[color=red]You need to be below Level {game.GameEvent.MaxLevel + 1} to join this lobby.[/color]");
            return;
        }

        if (game.GamePhase == EGamePhase.ENDING)
        {
            Utility.Say(player, $"[color=red]Game is ending[/color]");
            return;
        }
        
        Plugin.Instance.UI.HideMenuUI(gPlayer.Player);
        _ = Plugin.Instance.StartCoroutine(game.AddPlayerToGame(gPlayer));
    }

    public void RemovePlayerFromGame(UnturnedPlayer player)
    {
        var gPlayer = GetGamePlayer(player);
        if (gPlayer == null)
            return;

        if (gPlayer.CurrentGame == null)
        {
            Utility.Say(player, Plugin.Instance.Translate("Not_Ingame"));
            return;
        }

        gPlayer.CurrentGame.RemovePlayerFromGame(gPlayer);
        SendPlayerToLobby(player);
    }

    private void OnPlayerJoined(UnturnedPlayer player)
    {
        SendPlayerToLobby(player, showUI: false);
        _ = Plugin.Instance.StartCoroutine(DelayedJoin(player));
    }

    public IEnumerator DelayedJoin(UnturnedPlayer player)
    {
        var db = Plugin.Instance.DB;
        
        var transportConnection = player.Player.channel.owner.transportConnection;
        EffectManager.sendUIEffect(UIHandler.MAIN_MENU_ID, UIHandler.MAIN_MENU_KEY, transportConnection, true);
        var achievementImages = db.Achievements.SelectMany(k => k.Tiers).Select(k => k.TierPrevLarge).ToList();
        var achievementImage = 0;

        Plugin.Instance.UI.SendLoadingUI(player, false, null, "Syncing Data... (30 seconds)");
        if (!player.IsAdmin)
        {
            for (var i = 30; i >= 0; i--)
            {
                yield return new WaitForSeconds(1f);
            
                Plugin.Instance.UI.UpdateLoadingText(player, $"Syncing Data... ({i} seconds)");
                
                var maxAchievementImage = Math.Min(achievementImage + 15, achievementImages.Count);
                for (var j = achievementImage; j < maxAchievementImage; j++)
                {
                    EffectManager.sendUIEffectImageURL(UIHandler.MAIN_MENU_KEY, transportConnection, true, "Scene Loading IMAGE Box", achievementImages[j]);
                }

                achievementImage = maxAchievementImage;
            }
        }

        _ = Task.Run(async () =>
        {
            var avatarURL = "";
            var countryCode = "NNN";
            try
            {
                var profile = player.SteamProfile;
                avatarURL = $"{profile.AvatarIcon},{profile.AvatarMedium},{profile.AvatarFull}";
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting the steam profile for the player");
                Logger.Log(ex);
            }

            if (string.IsNullOrEmpty(avatarURL))
                avatarURL = "https://cdn.discordapp.com/attachments/458038940847439903/1026880604287012995/unknown.png,https://cdn.discordapp.com/attachments/458038940847439903/1026880604287012995/unknown.png,https://cdn.discordapp.com/attachments/458038940847439903/1026880604287012995/unknown.png";

            await db.AddPlayerAsync(player, player.CharacterName, avatarURL);
            await db.GetPlayerDataAsync(player);

            Plugin.Instance.UI.RegisterUIHandler(player);
            
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UI.ClearLoadingUI(player);
                player.Player.quests.leaveGroup(true);
                if (Players.ContainsKey(player.CSteamID))
                    _ = Players.Remove(player.CSteamID);

                Players.Add(player.CSteamID, new(player, player.Player.channel.GetOwnerTransportConnection()));
                Plugin.Instance.UI.ShowMenuUI(player);
            });
        });
    }

    private void OnPlayerLeft(UnturnedPlayer player)
    {
        Plugin.Instance.UI.UnregisterUIHandler(player);
        if (Players.TryGetValue(player.CSteamID, out var gPlayer))
        {
            var game = gPlayer.CurrentGame;
            game?.RemovePlayerFromGame(gPlayer);
            _ = Players.Remove(player.CSteamID);
        }

        Plugin.Instance.DB.PlayerData.Remove(player.CSteamID);
        Plugin.Instance.DB.PlayerLoadouts.Remove(player.CSteamID);
    }

    private void OnPlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
    {
        var ply = UnturnedPlayer.FromPlayer(sender.player);
        if (ply == null || !Players.TryGetValue(ply.CSteamID, out var gPlayer))
            return;

        var game = gPlayer.CurrentGame;
        if (game == null)
            SendPlayerToLobby(ply);
    }

    private void OnMessageSent(SteamPlayer player, EChatMode mode, ref Color chatted, ref bool isRich, string text, ref bool isVisible)
    {
        if (Players.TryGetValue(player.playerID.steamID, out var gPlayer))
        {
            var game = gPlayer.CurrentGame;
            if (game == null)
                isVisible = false;
        }
    }

    public void SendPlayerToLobby(UnturnedPlayer player, MatchEndSummary summary = null, bool showUI = true)
    {
        player.Player.inventory.ClearInventory();
        player.Player.life.serverModifyHealth(100);
        if (!player.GodMode)
            player.GodMode = true;
        player.Player.life.ServerRespawn(false);
        if (Config.Base.FileData.LobbySpawns.Count > 0)
        {
            var randomSpawn = Config.Base.FileData.LobbySpawns[UnityEngine.Random.Range(0, Config.Base.FileData.LobbySpawns.Count)];
            player.Player.teleportToLocationUnsafe(randomSpawn.GetSpawnPoint(), randomSpawn.Yaw);
        }
        if (showUI)
            Plugin.Instance.UI.ShowMenuUI(player, summary);
    }

    public (EGameType, GameEvent) GetRandomGameSetup(ArenaLocation location)
    {
        Logging.Debug($"Getting list of random gamemodes for location {location.LocationName}");
        var gameModeOptions = Config.Gamemode.FileData.GamemodeOptions.Where(k => !k.IgnoredLocations.Contains(location.LocationID)).ToList();
        Logging.Debug($"Found {gameModeOptions.Count} gamemode options to choose from");
        var option = CalculateRandomGameMode(gameModeOptions);
        GameEvent @event = null;
        foreach (var alwaysEvent in Config.Events.FileData.GameEvents.Where(k => k.AlwaysHaveLobby))
        {
            Logging.Debug($"Checking if there are enough {alwaysEvent.EventName} event lobbies");
            if (Games.Exists(k => k.GameEvent == alwaysEvent && k.Location.GetMaxPlayers(k.GameMode) > k.GetPlayerCount()))
                continue;

            Logging.Debug($"All other event lobbies of {alwaysEvent.EventName} are filled, setting this one to that event");
            @event = alwaysEvent;
            break;
        }

        if (@event != null)
            return (option.GameType, @event);
        
        Logging.Debug($"Getting list of random events for location {location.LocationName}");
        var events = Config.Events.FileData.GameEvents.Where(k => k.EventWeight != 0 && !k.IgnoredLocations.Contains(location.LocationID) && !k.IgnoredGameModes.Contains(option.GameType) && Games.Count(l => k == l.GameEvent) < k.EventLimit).ToList();
        Logging.Debug($"Found {events.Count} events to choose from");
        if (events.Count == 0)
            return (option.GameType, @event);

        var eventRNG = UnityEngine.Random.Range(1, 101);
        Logging.Debug($"Event RNG: {eventRNG}, Passed: {eventRNG <= option.EventChance}");
        @event = eventRNG <= option.EventChance ? CalculateRandomGameEvent(events) : null;

        return (option.GameType, @event);
    }

    private GameEvent CalculateRandomGameEvent(List<GameEvent> events)
    {
        var poolSize = events.Sum(k => k.EventWeight);
        var randInt = UnityEngine.Random.Range(0, poolSize) + 1;
        var accumulatedProbability = 0;
        foreach (var @event in events)
        {
            accumulatedProbability += @event.EventWeight;
            if (randInt <= accumulatedProbability)
                return @event;
        }

        return events[UnityEngine.Random.Range(0, events.Count)];
    }
    
    private GamemodeOption CalculateRandomGameMode(List<GamemodeOption> options)
    {
        var poolSize = options.Sum(option => option.GamemodeWeight);
        var randInt = UnityEngine.Random.Range(0, poolSize) + 1;
        Logging.Debug($"Gamemode RNG: {randInt}");
        var accumulatedProbability = 0;
        foreach (var option in options)
        {
            accumulatedProbability += option.GamemodeWeight;
            if (randInt <= accumulatedProbability)
                return option;
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

    public IEnumerator CheckGames()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);

            try
            {
                Logging.Debug($"Games: {Games.Count}, Minimum Games: {Config.Base.FileData.MinGamesCount}, Maximum Games: {Config.Base.FileData.MaxGamesCount}");
                if (Games.Count == Config.Base.FileData.MaxGamesCount)
                    continue;
            
                var gameCount = Games.Sum(k => k.GetPlayerCount());
                var gameMaxCount = Games.Sum(k => k.Location.GetMaxPlayers(k.GameMode));

                var threshold = gameCount * 100 / gameMaxCount;
                Logging.Debug($"Checking games, total games: {Games.Count}, players: {gameCount}, max count: {gameMaxCount}, threshold: {threshold}, game threshold: {Config.Base.FileData.GameThreshold}");
                var ignoreThreshold = false;
                foreach (var alwaysEvent in Config.Events.FileData.GameEvents.Where(k => k.AlwaysHaveLobby))
                {
                    Logging.Debug($"Checking if there are enough {alwaysEvent.EventName} event lobbies");
                    if (Games.Exists(k => k.GameEvent == alwaysEvent && k.Location.GetMaxPlayers(k.GameMode) > k.GetPlayerCount()))
                        continue;

                    Logging.Debug($"All other event lobbies of {alwaysEvent.EventName} are filled, setting this one to that event");
                    ignoreThreshold = true;
                    break;
                }
                
                if (!ignoreThreshold && threshold < Config.Base.FileData.GameThreshold)
                    continue;
            
                Logging.Debug($"Percentage above {Config.Base.FileData.GameThreshold}, creating new game");
                lock (AvailableLocations)
                {
                    Logging.Debug($"{AvailableLocations.Count} locations available");
                    if (AvailableLocations.Count == 0)
                    {
                        Logging.Debug("No locations available to start game, returning");
                        continue;
                    }
                
                    var locationID = AvailableLocations[UnityEngine.Random.Range(0, AvailableLocations.Count)];
                    var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
                    if (location == null)
                    {
                        Logging.Debug("No locations available to start game, returning");
                        continue;
                    }
                
                    var gameSetup = GetRandomGameSetup(location);
                    Logging.Debug($"Found Location: {location.LocationName}, GameMode: {gameSetup.Item1}, Event: {gameSetup.Item2?.EventName ?? "None"}");
                    StartGame(location, gameSetup.Item1, gameSetup.Item2, false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error checking games");
                Logger.Log(ex);
            }
        }
    }
}