using Rocket.Core.Utils;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.Webhook;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout.GameTypes;

public class CTFGame : Game
{
    public Dictionary<int, List<CTFSpawnPoint>> SpawnPoints { get; set; }

    public List<CTFPlayer> Players { get; set; }
    public Dictionary<CSteamID, CTFPlayer> PlayersLookup { get; set; }

    public CTFTeam BlueTeam { get; set; }
    public CTFTeam RedTeam { get; set; }

    public Coroutine GameStarter { get; set; }
    public Coroutine GameEnder { get; set; }

    public Coroutine FlagChecker { get; set; }

    public uint Frequency { get; set; }

    public CTFGame(ArenaLocation location, bool isHardcore) : base(EGameType.CTF, location, isHardcore)
    {
        SpawnPoints = new();
        var blueFlag = Vector3.zero;
        var redFlag = Vector3.zero;

        foreach (var spawnPoint in Plugin.Instance.Data.Data.CTFSpawnPoints.Where(k => k.LocationID == location.LocationID))
        {
            if (spawnPoint.IsFlagSP)
            {
                if ((ETeam)spawnPoint.GroupID == ETeam.BLUE)
                    blueFlag = spawnPoint.GetSpawnPoint();
                else if ((ETeam)spawnPoint.GroupID == ETeam.RED)
                    redFlag = spawnPoint.GetSpawnPoint();

                continue;
            }

            if (SpawnPoints.TryGetValue(spawnPoint.GroupID, out var spawnPoints))
                spawnPoints.Add(spawnPoint);
            else
                SpawnPoints.Add(spawnPoint.GroupID, new() { spawnPoint });
        }

        Players = new();
        PlayersLookup = new();

        var blueTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
        var redTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

        BlueTeam = new((byte)ETeam.BLUE, false, blueTeamInfo, Config.CTF.FileData.BlueFlagID, blueFlag);
        RedTeam = new((byte)ETeam.RED, false, redTeamInfo, Config.CTF.FileData.RedFlagID, redFlag);
        Frequency = Utility.GetFreeFrequency();
    }


    public override void ForceStartGame()
    {
        GameStarter = Plugin.Instance.StartCoroutine(StartGame());
    }

    public override void ForceEndGame()
    {
        var wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam : RedTeam.Score > BlueTeam.Score ? RedTeam : new(-1, true, new(), 0, Vector3.zero);
        _ = Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
    }

    public IEnumerator StartGame()
    {
        GameChecker.Stop();
        GamePhase = EGamePhase.STARTING;
        foreach (var player in Players)
        {
            if (player.GamePlayer.IsLoading)
                continue;

            UI.ClearWaitingForPlayersUI(player.GamePlayer);
            player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
            UI.ShowCountdownUI(player.GamePlayer);
            SpawnPlayer(player);
            if (player.GamePlayer.IsPendingLoadoutChange)
            {
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    Plugin.Instance.Loadout.GiveLoadout(player.GamePlayer);
                    player.GamePlayer.IsPendingLoadoutChange = false;
                });
            }
        }

        for (var seconds = Config.CTF.FileData.StartSeconds; seconds >= 0; seconds--)
        {
            yield return new WaitForSeconds(1);

            foreach (var player in Players)
                UI.SendCountdownSeconds(player.GamePlayer, seconds);
        }

        GamePhase = EGamePhase.STARTED;
        foreach (var player in Players)
        {
            player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            player.StartTime = DateTime.UtcNow;
            UI.ClearCountdownUI(player.GamePlayer);
        }

        UI.SendCTFHUD(BlueTeam, RedTeam, Players);

        TaskDispatcher.QueueOnMainThread(() =>
        {
            CleanMap();
            ItemManager.dropItem(new(RedTeam.FlagID, true), RedTeam.FlagSP, true, true, true);
            ItemManager.dropItem(new(BlueTeam.FlagID, true), BlueTeam.FlagSP, true, true, true);
        });

        GameEnder = Plugin.Instance.StartCoroutine(EndGame());
    }

    public IEnumerator EndGame()
    {
        for (var seconds = Config.CTF.FileData.EndSeconds; seconds >= 0; seconds--)
        {
            yield return new WaitForSeconds(1);

            var timeSpan = TimeSpan.FromSeconds(seconds);
            foreach (var player in Players)
                UI.UpdateCTFTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
        }

        var wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam : RedTeam.Score > BlueTeam.Score ? RedTeam : new(-1, true, new(), 0, Vector3.zero);
        _ = Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
    }

    public IEnumerator GameEnd(CTFTeam wonTeam)
    {
        GameEnder.Stop();
        GamePhase = EGamePhase.ENDING;
        UI.OnGameUpdated();

        var endTime = DateTime.UtcNow;
        List<GamePlayer> roundEndCasesPlayers = new();
        Dictionary<GamePlayer, MatchEndSummary> summaries = new();
        List<(GamePlayer, Case)> roundEndCases = new();
        
        try
        {
            foreach (var player in Players)
            {
                var totalMinutesPlayed = (int)(endTime - player.StartTime).TotalMinutes;
                if (totalMinutesPlayed < Config.RoundEndCases.FileData.MinimumMinutesPlayed || player.Kills == 0)
                    continue;

                var chance = Config.RoundEndCases.FileData.Chance * totalMinutesPlayed;
                if (UnityEngine.Random.Range(1, 101) > chance)
                    continue;

                roundEndCasesPlayers.Add(player.GamePlayer);
                if (roundEndCasesPlayers.Count == 8)
                    break;
            }

            foreach (var roundEndCasePlayer in roundEndCasesPlayers)
            {
                var @case = GetRandomRoundEndCase();
                if (@case == null)
                    continue;

                Plugin.Instance.Discord.SendEmbed(
                    new(null, null, null, Utility.GetDiscordColorCode(@case.CaseRarity), DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon),
                        new(roundEndCasePlayer.Data.SteamName, $"https://steamcommunity.com/profiles/{roundEndCasePlayer.SteamID}", roundEndCasePlayer.Data.AvatarLinks[0]), new Field[] { new($"[{@case.CaseRarity.ToFriendlyName()}] {@case.CaseName}", "‎", true) }, new(@case.IconLink), null), null,
                    Plugin.Instance.Config.Webhooks.FileData.CaseDroppedWebhookLink);

                roundEndCases.Add((roundEndCasePlayer, @case));
                DB.IncreasePlayerCase(roundEndCasePlayer.SteamID, @case.CaseID, 1);
            }

            foreach (var player in Players)
            {
                UI.ClearCTFHUD(player.GamePlayer);
                UI.ClearMidgameLoadoutUI(player.GamePlayer);
                if (player.GamePlayer.Player.Player.life.isDead)
                    player.GamePlayer.Player.Player.life.ServerRespawn(false);

                UI.RemoveKillCard(player.GamePlayer);
                UI.ClearAnimations(player.GamePlayer);

                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    UI.HideCTFLeaderboard(player.GamePlayer);
                }

                MatchEndSummary summary = new(player.GamePlayer, player.XP, player.StartingLevel, player.StartingXP, player.Kills, player.Deaths, player.Assists, player.HighestKillstreak, player.HighestMK, player.StartTime, GameMode, player.Team == wonTeam);
                summaries.Add(player.GamePlayer, summary);
                DB.IncreasePlayerXP(player.GamePlayer.SteamID, summary.PendingXP);
                DB.IncreasePlayerCredits(player.GamePlayer.SteamID, summary.PendingCredits);
                DB.IncreasePlayerBPXP(player.GamePlayer.SteamID, summary.BattlepassXP + summary.BattlepassBonusXP);
                TaskDispatcher.QueueOnMainThread(() => Quest.CheckQuest(player.GamePlayer, EQuestType.FINISH_MATCH,
                    new()
                    {
                        { EQuestCondition.MAP, Location.LocationID },
                        { EQuestCondition.GAMEMODE, (int)GameMode },
                        { EQuestCondition.WIN_FLAGS_CAPTURED, player.FlagsCaptured },
                        { EQuestCondition.WIN_FLAGS_SAVED, player.FlagsSaved },
                        { EQuestCondition.WIN_KILLS, player.Kills }
                    }));

                if (player.Team == wonTeam)
                {
                    TaskDispatcher.QueueOnMainThread(() => Quest.CheckQuest(player.GamePlayer, EQuestType.WIN,
                        new()
                        {
                            { EQuestCondition.MAP, Location.LocationID },
                            { EQuestCondition.GAMEMODE, (int)GameMode },
                            { EQuestCondition.WIN_FLAGS_CAPTURED, player.FlagsCaptured },
                            { EQuestCondition.WIN_FLAGS_SAVED, player.FlagsSaved },
                            { EQuestCondition.WIN_KILLS, player.Kills }
                        }));
                }

                UI.SetupPreEndingUI(player.GamePlayer, EGameType.CTF, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName, wonTeam.TeamID == -1);
                player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            }

            TaskDispatcher.QueueOnMainThread(() =>
            {
                UI.SetupCTFLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false, IsHardcore);
                CleanMap();
            });
        }
        catch (Exception ex)
        {
            Logger.Log($"Error ending game on location {Location.LocationName} and gamemode {GameMode}. Part: Setting up match end summaries and round end cases");
            Logger.Log(ex);
        }

        yield return new WaitForSeconds(5);

        try
        {
            foreach (var player in Players)
                UI.ShowCTFLeaderboard(player.GamePlayer);

            if (roundEndCases.Count > 0)
                _ = Plugin.Instance.StartCoroutine(UI.SetupRoundEndDrops(Players.Select(k => k.GamePlayer).ToList(), roundEndCases, 2));
        }
        catch (Exception ex)
        {
            Logger.Log($"Error ending game on location {Location.LocationName} and gamemode {GameMode}. Part: Sending leaderboard to players and setting up round end cases.");
            Logger.Log(ex);
        }

        yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds - 3);

        try
        {
            foreach (var player in Players)
                player.GamePlayer.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        }
        catch (Exception ex)
        {
            Logger.Log($"Error ending game on location {Location.LocationName} and gamemode {GameMode}. Part: Sending widget flag to players for 3 seconds before teleportation commences");
            Logger.Log(ex);
        }

        yield return new WaitForSeconds(3);

        try
        {
            foreach (var player in Players.ToList())
            {
                var gPlayer = player.GamePlayer;
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.Game.SendPlayerToLobby(gPlayer.Player, summaries.TryGetValue(gPlayer, out var pendingSummary) ? pendingSummary : null);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error ending game on location {Location.LocationName} and gamemode {GameMode}. Part: Removing players from the game and tping them off the map");
            Logger.Log(ex);
        }

        Players.Clear();
        PlayersLookup.Clear();
        BlueTeam.Destroy();
        RedTeam.Destroy();

        BlueTeam = null;
        RedTeam = null;
        
        var locations = Plugin.Instance.Game.AvailableLocations;
        lock (locations)
        {
            var randomLocation = locations.Count > 0 ? locations[UnityEngine.Random.Range(0, locations.Count)] : Location.LocationID;
            var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == randomLocation);
            var gameMode = Plugin.Instance.Game.GetRandomGameMode(location.LocationID);
            GamePhase = EGamePhase.ENDED;
            Plugin.Instance.Game.EndGame(this);
            Plugin.Instance.Game.StartGame(location, gameMode.Item1, gameMode.Item2);
        }
    }

    public override IEnumerator AddPlayerToGame(GamePlayer player)
    {
        if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            yield break;

        player.OnGameJoined(this);
        var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;
        CTFPlayer cPlayer = new(player, team);
        team.AddPlayer(player.SteamID);
        Players.Add(cPlayer);
        if (PlayersLookup.ContainsKey(player.SteamID))
            _ = PlayersLookup.Remove(player.SteamID);

        PlayersLookup.Add(player.SteamID, cPlayer);

        UI.OnGameCountUpdated(this);
        UI.SendLoadingUI(player.Player, true, GameMode, Location);
        for (var seconds = 1; seconds <= 5; seconds++)
        {
            yield return new WaitForSeconds(1);

            UI.UpdateLoadingBar(player.Player, new('　', Math.Min(96, seconds * 96 / 5)));
        }

        if (player.Player.GodMode)
            player.Player.GodMode = false;
        var currentPos = player.Player.Position;
        player.Player.Player.teleportToLocationUnsafe(new(currentPos.x, currentPos.y + 100, currentPos.z), 0);
        GiveLoadout(cPlayer);
        UI.SendPreEndingUI(cPlayer.GamePlayer);
        SpawnPlayer(cPlayer);
        UI.ClearLoadingUI(player.Player);
        UI.SendVoiceChatUI(player);

        player.IsLoading = false;
        switch (GamePhase)
        {
            case EGamePhase.WAITING_FOR_PLAYERS:
                var minPlayers = Location.GetMinPlayers(GameMode);
                if (Players.Count >= minPlayers)
                    GameStarter = Plugin.Instance.StartCoroutine(StartGame());
                else
                {
                    UI.SendWaitingForPlayersUI(player, Players.Count, minPlayers);
                    GameChecker.Stop();
                    foreach (var ply in Players)
                    {
                        if (ply == cPlayer)
                            continue;

                        UI.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
                    }
                }

                break;
            case EGamePhase.STARTING:
                player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                UI.ShowCountdownUI(player);
                break;
            case EGamePhase.ENDING:
                var wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam : RedTeam.Score > BlueTeam.Score ? RedTeam : new(-1, true, new(), 0, Vector3.zero);
                UI.SetupCTFLeaderboard(cPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
                UI.ShowCTFLeaderboard(cPlayer.GamePlayer);
                break;
            default:
                UI.SendCTFHUD(cPlayer, BlueTeam, RedTeam, Players);
                break;
        }
    }

    public override void RemovePlayerFromGame(GamePlayer player)
    {
        if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            return;

        var cPlayer = GetCTFPlayer(player.Player);

        UI.ClearCTFHUD(player);
        UI.ClearPreEndingUI(player);
        UI.ClearVoiceChatUI(player);
        UI.ClearKillstreakUI(player);

        OnStoppedTalking(player);

        switch (GamePhase)
        {
            case EGamePhase.STARTING:
                UI.ClearCountdownUI(player);
                cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
                break;
            case EGamePhase.WAITING_FOR_PLAYERS:
                UI.ClearWaitingForPlayersUI(player);
                break;
        }

        if (GamePhase != EGamePhase.ENDING)
        {
            TaskDispatcher.QueueOnMainThread(() =>
                BarricadeManager.BarricadeRegions.Cast<BarricadeRegion>().SelectMany(k => k.drops).Where(k => (k.GetServersideData()?.owner ?? 0UL) == player.SteamID.m_SteamID && LevelNavigation.tryGetNavigation(k.model.transform.position, out var nav) && nav == Location.NavMesh)
                    .Select(k => BarricadeManager.tryGetRegion(k.model.transform, out var x, out var y, out var plant, out var _) ? (k, x, y, plant) : (k, byte.MaxValue, byte.MaxValue, ushort.MaxValue)).ToList().ForEach(k => BarricadeManager.destroyBarricade(k.k, k.Item2, k.Item3, k.Item4)));
        }
        
        cPlayer.Team.RemovePlayer(cPlayer.GamePlayer.SteamID);
        cPlayer.GamePlayer.OnGameLeft();
        _ = Players.Remove(cPlayer);
        _ = PlayersLookup.Remove(cPlayer.GamePlayer.SteamID);

        if (cPlayer.IsCarryingFlag)
        {
            var otherTeam = cPlayer.Team.TeamID == BlueTeam.TeamID ? RedTeam : BlueTeam;
            if (cPlayer.GamePlayer.Player.Player.clothing.backpack == otherTeam.FlagID)
                ItemManager.dropItem(new(otherTeam.FlagID, true), cPlayer.GamePlayer.Player.Player.transform.position, true, true, true);

            cPlayer.IsCarryingFlag = false;
            cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1f);
            TaskDispatcher.QueueOnMainThread(() =>
            {
                UI.UpdateCTFHUD(Players, otherTeam);
                UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.DROPPED);
            });
        }

        cPlayer.Destroy();

        UI.OnGameCountUpdated(this);
        
        if (GamePhase != EGamePhase.ENDING)
            return;
        
        if (Players.Count == 0)
        {
            GameChecker.Stop();
            GameChecker = Plugin.Instance.StartCoroutine(CheckGame());
        }
        else
        {
            var minPlayers = Location.GetMinPlayers(GameMode);
            foreach (var ply in Players)
                UI.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
        }
    }

    public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
    {
        var cPlayer = GetCTFPlayer(player);
        if (cPlayer == null)
            return;

        if (cause == EDeathCause.SUICIDE)
        {
            if (GamePhase == EGamePhase.ENDING)
            {
                TaskDispatcher.QueueOnMainThread(() => player.life.ServerRespawn(false));
                return;
            }
            
            RemovePlayerFromGame(cPlayer.GamePlayer);
            return;
        }

        if (cPlayer.GamePlayer.HasScoreboard)
        {
            cPlayer.GamePlayer.HasScoreboard = false;
            UI.HideCTFLeaderboard(cPlayer.GamePlayer);
        }

        var victimKS = cPlayer.Killstreak;
        Logging.Debug($"Game player died, player name: {cPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");

        var updatedKiller = cause == EDeathCause.WATER ? cPlayer.GamePlayer.SteamID : cause is EDeathCause.LANDMINE or EDeathCause.SHRED ? cPlayer.GamePlayer.LastDamager.Count > 0 ? cPlayer.GamePlayer.LastDamager.Pop() : killer : killer;

        cPlayer.OnDeath(updatedKiller);
        cPlayer.GamePlayer.OnDeath(updatedKiller, Config.CTF.FileData.RespawnSeconds);

        var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;
        var isFlagCarrier = false;

        if (cPlayer.IsCarryingFlag)
        {
            isFlagCarrier = true;
            if (player.clothing.backpack == otherTeam.FlagID)
                ItemManager.dropItem(new(otherTeam.FlagID, true), cause == EDeathCause.WATER ? otherTeam.FlagSP : player.transform.position, true, true, true);

            cPlayer.IsCarryingFlag = false;
            cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1f);
            TaskDispatcher.QueueOnMainThread(() =>
            {
                UI.UpdateCTFHUD(Players, otherTeam);
                UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.DROPPED);
            });
        }

        DB.IncreasePlayerDeaths(cPlayer.GamePlayer.SteamID, 1);
        TaskDispatcher.QueueOnMainThread(() =>
        {
            var kPlayer = GetCTFPlayer(updatedKiller);
            if (kPlayer == null)
            {
                Logging.Debug("Killer not found, returning");
                return;
            }

            if (kPlayer.GamePlayer.SteamID == cPlayer.GamePlayer.SteamID)
            {
                OnKill(kPlayer.GamePlayer, cPlayer.GamePlayer, 0, kPlayer.Team.Info.KillFeedHexCode, cPlayer.Team.Info.KillFeedHexCode, false, cause == EDeathCause.WATER ? SUICIDE_SYMBOL : EXPLOSION_SYMBOL);

                Logging.Debug("Player killed themselves, returning");
                return;
            }

            Dictionary<EQuestCondition, int> questConditions = new() { { EQuestCondition.MAP, Location.LocationID }, { EQuestCondition.GAMEMODE, (int)GameMode } };

            Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");

            if (cPlayer.GamePlayer.LastDamager.Count > 0 && cPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                _ = cPlayer.GamePlayer.LastDamager.Pop();

            if (cPlayer.GamePlayer.LastDamager.Count > 0)
            {
                var assister = GetCTFPlayer(cPlayer.GamePlayer.LastDamager.Pop());
                if (assister != null && assister != kPlayer)
                {
                    assister.Assists++;
                    assister.Score += Config.Points.FileData.AssistPoints;
                    if (!assister.GamePlayer.Player.Player.life.isDead)
                        UI.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP, Plugin.Instance.Translate("Assist_Kill", cPlayer.GamePlayer.Player.CharacterName.ToUnrich()));

                    DB.IncreasePlayerXP(assister.GamePlayer.SteamID, Config.Medals.FileData.AssistKillXP);
                }

                cPlayer.GamePlayer.LastDamager.Clear();
            }

            var isFirstKill = Players[0].Kills == 0;
            kPlayer.Kills++;
            kPlayer.Score += Config.Points.FileData.KillPoints;

            var xpGained = 0;
            var xpText = "";

            ushort equipmentUsed = 0;
            var longshotRange = 0f;

            var usedKillstreak = kPlayer.GamePlayer.HasKillstreakActive && (kPlayer.GamePlayer.ActiveKillstreak?.Killstreak?.KillstreakInfo?.IsItem ?? false) && cause != EDeathCause.SENTRY;
            var killstreakID = kPlayer.GamePlayer.ActiveKillstreak?.Killstreak?.KillstreakID ?? 0;

            string overrideSymbol = null;
            if (usedKillstreak)
            {
                var info = kPlayer.GamePlayer.ActiveKillstreak.Killstreak.KillstreakInfo;
                xpGained += info.MedalXP;
                xpText += info.MedalName;
                equipmentUsed += info.ItemID;
                questConditions.Add(EQuestCondition.KILLSTREAK, killstreakID);
            }
            else
            {
                switch (cause)
                {
                    case EDeathCause.MELEE:
                        xpGained += Config.Medals.FileData.MeleeKillXP;
                        xpText += Plugin.Instance.Translate("Melee_Kill").ToRich();
                        equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Knife?.Knife?.KnifeID ?? 0;
                        overrideSymbol = MELEE_SYMBOL;
                        questConditions.Add(EQuestCondition.KNIFE, equipmentUsed);
                        break;
                    case EDeathCause.GUN:
                        if (limb == ELimb.SKULL)
                        {
                            xpGained += Config.Medals.FileData.HeadshotKillXP;
                            xpText += Plugin.Instance.Translate("Headshot_Kill").ToRich();
                        }
                        else
                        {
                            xpGained += Config.Medals.FileData.NormalKillXP;
                            xpText += Plugin.Instance.Translate("Normal_Kill").ToRich();
                        }

                        var equipment = kPlayer.GamePlayer.Player.Player.equipment.itemID;
                        if (equipment == (kPlayer.GamePlayer.ActiveLoadout.PrimarySkin?.SkinID ?? 0) || equipment == (kPlayer.GamePlayer.ActiveLoadout.Primary?.Gun?.GunID ?? 0))
                        {
                            questConditions.Add(EQuestCondition.GUN_TYPE, (int)kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunType);
                            equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunID;
                            longshotRange = kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.LongshotRange;
                        }
                        else if (equipment == (kPlayer.GamePlayer.ActiveLoadout.SecondarySkin?.SkinID ?? 0) || equipment == (kPlayer.GamePlayer.ActiveLoadout.Secondary?.Gun?.GunID ?? 0))
                        {
                            questConditions.Add(EQuestCondition.GUN_TYPE, (int)kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunType);
                            equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunID;
                            longshotRange = kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.LongshotRange;
                        }
                        else
                            equipmentUsed = equipment;

                        questConditions.Add(EQuestCondition.GUN, equipmentUsed);
                        break;
                    case EDeathCause.CHARGE:
                    case EDeathCause.GRENADE:
                    case EDeathCause.LANDMINE:
                    case EDeathCause.BURNING:
                    case EDeathCause.SHRED:
                        xpGained += Config.Medals.FileData.LethalKillXP;
                        xpText += Plugin.Instance.Translate("Lethal_Kill").ToRich();
                        equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0;
                        questConditions.Add(EQuestCondition.GADGET, equipmentUsed);
                        break;
                    case EDeathCause.SENTRY:
                        if (!GameTurrets.TryGetValue(kPlayer.GamePlayer, out var sentry))
                            break;

                        equipmentUsed = sentry.Item1.asset.id;
                        xpGained += sentry.Item2.MedalXP;
                        xpText += sentry.Item2.MedalName;
                        break;
                }
            }

            xpText += "\n";

            kPlayer.SetKillstreak(kPlayer.Killstreak + 1);
            questConditions.Add(EQuestCondition.TARGET_KS, kPlayer.Killstreak);
            if (kPlayer.MultipleKills == 0)
                kPlayer.SetMultipleKills(kPlayer.MultipleKills + 1);
            else if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
            {
                kPlayer.SetMultipleKills(kPlayer.MultipleKills + 1);
                xpGained += Config.Medals.FileData.BaseXPMK + kPlayer.MultipleKills * Config.Medals.FileData.IncreaseXPPerMK;
                var multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
                xpText += (multiKillText == $"Multiple_Kills_Show_{kPlayer.MultipleKills}" ? Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich() : multiKillText) + "\n";
            }
            else
                kPlayer.SetMultipleKills(1);

            questConditions.Add(EQuestCondition.TARGET_MK, kPlayer.MultipleKills);

            if (victimKS > Config.Medals.FileData.ShutdownKillStreak)
            {
                xpGained += Config.Medals.FileData.ShutdownXP;
                xpText += Plugin.Instance.Translate("Shutdown_Kill").ToRich() + "\n";
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.SHUTDOWN, questConditions);
            }

            if (kPlayer.PlayersKilled.ContainsKey(cPlayer.GamePlayer.SteamID))
            {
                kPlayer.PlayersKilled[cPlayer.GamePlayer.SteamID] += 1;
                if (kPlayer.PlayersKilled[cPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                {
                    xpGained += Config.Medals.FileData.DominationXP;
                    xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                    Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.DOMINATION, questConditions);
                }
            }
            else
                kPlayer.PlayersKilled.Add(cPlayer.GamePlayer.SteamID, 1);

            if (isFlagCarrier)
            {
                xpGained += Config.Medals.FileData.FlagCarrierKilledXP;
                xpText += Plugin.Instance.Translate("Flag_Killer").ToRich() + "\n";
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.FLAG_KILLER, questConditions);
            }

            if (kPlayer.IsCarryingFlag)
            {
                xpGained += Config.Medals.FileData.KillWhileCarryingFlagXP;
                xpText += Plugin.Instance.Translate("Flag_Denied").ToRich() + "\n";
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.FLAG_DENIED, questConditions);
            }

            if (cPlayer.GamePlayer.SteamID == kPlayer.GamePlayer.LastKiller)
            {
                xpGained += Config.Medals.FileData.RevengeXP;
                xpText += Plugin.Instance.Translate("Revenge_Kill").ToRich() + "\n";
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.REVENGE, questConditions);
            }

            if (isFirstKill)
            {
                xpGained += Config.Medals.FileData.FirstKillXP;
                xpText += Plugin.Instance.Translate("First_Kill").ToRich() + "\n";
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.FIRST_KILL, questConditions);
            }

            if (!usedKillstreak && cause == EDeathCause.GUN && (cPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
            {
                xpGained += Config.Medals.FileData.LongshotXP;
                xpText += Plugin.Instance.Translate("Longshot_Kill").ToRich() + "\n";
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.LONGSHOT, questConditions);
            }

            if (kPlayer.GamePlayer.Player.Player.life.health < Config.Medals.FileData.HealthSurvivorKill)
            {
                xpGained += Config.Medals.FileData.SurvivorXP;
                xpText += Plugin.Instance.Translate("Survivor_Kill").ToRich() + "\n";
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.SURVIVOR, questConditions);
            }

            kPlayer.GamePlayer.LastKiller = CSteamID.Nil;
            kPlayer.LastKill = DateTime.UtcNow;
            kPlayer.XP += xpGained;
            Players.Sort((x, y) => y.Kills.CompareTo(x.Kills));

            UI.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);
            UI.SendMultiKillSound(kPlayer.GamePlayer, kPlayer.MultipleKills);
            kPlayer.CheckKills();
            kPlayer.GamePlayer.OnKilled(cPlayer.GamePlayer);

            if (equipmentUsed != 0)
                OnKill(kPlayer.GamePlayer, cPlayer.GamePlayer, equipmentUsed, kPlayer.Team.Info.KillFeedHexCode, cPlayer.Team.Info.KillFeedHexCode, cause == EDeathCause.GUN && limb == ELimb.SKULL, overrideSymbol);

            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.KILL, questConditions);
            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.MULTI_KILL, questConditions);
            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.KILLSTREAK, questConditions);
            if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.HEADSHOTS, questConditions);

            Quest.CheckQuest(cPlayer.GamePlayer, EQuestType.DEATH, questConditions);

            DB.IncreasePlayerXP(kPlayer.GamePlayer.SteamID, xpGained);
            if (cause == EDeathCause.GUN && limb == ELimb.SKULL)
                DB.IncreasePlayerHeadshotKills(kPlayer.GamePlayer.SteamID, 1);
            else
                DB.IncreasePlayerKills(kPlayer.GamePlayer.SteamID, 1);

            if (usedKillstreak)
                DB.IncreasePlayerKillstreakKills(kPlayer.GamePlayer.SteamID, killstreakID, 1);
            else if ((kPlayer.GamePlayer.ActiveLoadout.Primary != null && kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunID == equipmentUsed) || (kPlayer.GamePlayer.ActiveLoadout.Secondary != null && kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunID == equipmentUsed))
            {
                DB.IncreasePlayerGunXP(kPlayer.GamePlayer.SteamID, equipmentUsed, xpGained);
                DB.IncreasePlayerGunKills(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
            }
            else if (kPlayer.GamePlayer.ActiveLoadout.Lethal != null && kPlayer.GamePlayer.ActiveLoadout.Lethal.Gadget.GadgetID == equipmentUsed)
                DB.IncreasePlayerGadgetKills(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
            else if (kPlayer.GamePlayer.ActiveLoadout.Knife != null && kPlayer.GamePlayer.ActiveLoadout.Knife.Knife.KnifeID == equipmentUsed)
                DB.IncreasePlayerKnifeKills(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
        });
    }

    public override void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow)
    {
        var player = GetCTFPlayer(parameters.player);
        if (player == null)
            return;

        parameters.applyGlobalArmorMultiplier = IsHardcore;
        if (GamePhase != EGamePhase.STARTED)
        {
            shouldAllow = false;
            return;
        }

        if (player.GamePlayer.HasSpawnProtection)
        {
            Logging.Debug($"{player.GamePlayer.Player.CharacterName} got damaged, but damaged got ignored due to the player having spawn prot. {player.GamePlayer.SpawnProtectionRemover == null}");
            shouldAllow = false;
            return;
        }

        var damageReducePerkName = "none";
        var damageIncreasePerkName = "none";
        switch (parameters.cause)
        {
            case EDeathCause.SENTRY:
            case EDeathCause.GUN:
                damageReducePerkName = "bulletproof";
                damageIncreasePerkName = "gundamage";
                break;
            case EDeathCause.CHARGE:
            case EDeathCause.GRENADE:
            case EDeathCause.LANDMINE:
            case EDeathCause.BURNING:
            case EDeathCause.SHRED:
                damageReducePerkName = "tank";
                damageIncreasePerkName = "lethaldamage";
                break;
        }

        parameters.damage -= (player.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageReducePerkName, out var damageReducerPerk) ? (float)damageReducerPerk.Perk.SkillLevel / 100 : 0f) * parameters.damage;

        player.GamePlayer.OnDamaged(parameters.killer);

        var kPlayer = GetCTFPlayer(parameters.killer);
        if (kPlayer == null)
            return;

        if (kPlayer.Team == player.Team && kPlayer != player)
        {
            shouldAllow = false;
            return;
        }

        parameters.damage += (kPlayer.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageIncreasePerkName, out var damageIncreaserPerk) ? (float)damageIncreaserPerk.Perk.SkillLevel / 100 : 0f) * parameters.damage;

        if (parameters.cause == EDeathCause.GRENADE && kPlayer != player)
        {
            var times = parameters.times;
            if (parameters.respectArmor)
                times *= DamageTool.getPlayerArmor(parameters.limb, parameters.player);

            if (parameters.applyGlobalArmorMultiplier)
                times *= Provider.modeConfigData.Players.Armor_Multiplier;

            var damage = Mathf.FloorToInt(parameters.damage * times);
            var finalDamage = (byte)Mathf.Min(255, damage);
        
            Logging.Debug($"cause: {parameters.cause}, damage: {parameters.damage}, final damage: {finalDamage}, player health: {player.GamePlayer.Player.Player.life.health}");
            if (finalDamage < player.GamePlayer.Player.Player.life.health)
            {
                Logging.Debug($"Condition fulfilled, send hit xp for {Config.Medals.FileData.LethalHitXP}");
                UI.ShowXPUI(kPlayer.GamePlayer, Config.Medals.FileData.LethalHitXP, Plugin.Instance.Translate("Lethal_Hit"));
            }
        }

        if (kPlayer.GamePlayer.HasSpawnProtection)
        {
            kPlayer.GamePlayer.SpawnProtectionRemover.Stop();
            kPlayer.GamePlayer.HasSpawnProtection = false;
        }
    }

    public override void OnPlayerRevived(UnturnedPlayer player)
    {
        var cPlayer = GetCTFPlayer(player);
        if (cPlayer == null)
            return;

        var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;
        if (otherTeam.FlagID == player.Player.clothing.backpack)
        {
            player.Player.clothing.thirdClothes.backpack = 0;
            player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);
        }

        cPlayer.GamePlayer.OnRevived();
    }

    public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition, ref float yaw)
    {
        var cPlayer = GetCTFPlayer(player.Player);
        if (cPlayer == null)
            return;

        if (!SpawnPoints.TryGetValue(cPlayer.Team.SpawnPoint, out var spawnPoints))
            return;

        if (spawnPoints.Count == 0)
            return;

        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        respawnPosition = spawnPoint.GetSpawnPoint();
        yaw = spawnPoint.Yaw;
        player.GiveSpawnProtection(Config.CTF.FileData.SpawnProtectionSeconds);
    }

    public override void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow)
    {
        var cPlayer = GetCTFPlayer(player.Player);
        if (cPlayer == null)
            return;

        Logging.Debug($"{player.Player.CharacterName} is trying to pick up item {itemData.item.id}");
        var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;

        if (player.Player.Player.equipment.isBusy)
        {
            shouldAllow = false;
            return;
        }

        Dictionary<EQuestCondition, int> questConditions = new() { { EQuestCondition.MAP, Location.LocationID }, { EQuestCondition.GAMEMODE, (int)GameMode } };

        if (cPlayer.Team.FlagID == itemData.item.id)
        {
            Logging.Debug($"{player.Player.CharacterName} is trying to pick up their own flag, checking if they are saving the flag");
            shouldAllow = false;

            if (!cPlayer.Team.HasFlag)
            {
                Logging.Debug($"{player.Player.CharacterName} is saving their flag, clearing the flag and putting it back into position");
                Logging.Debug($"Spawning their team's flag at {cPlayer.Team.FlagSP} for location {Location.LocationName}");
                ItemManager.ServerClearItemsInSphere(itemData.point, 1);
                ItemManager.dropItem(new(cPlayer.Team.FlagID, true), cPlayer.Team.FlagSP, true, true, true);
                cPlayer.Team.HasFlag = true;
                cPlayer.Score += Config.Points.FileData.FlagSavedPoints;
                cPlayer.XP += Config.Medals.FileData.FlagSavedXP;
                cPlayer.FlagsSaved++;
                UI.ShowXPUI(cPlayer.GamePlayer, Config.Medals.FileData.FlagSavedXP, Plugin.Instance.Translate("Flag_Saved").ToRich());
                UI.SendFlagSavedSound(cPlayer.GamePlayer);

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    UI.UpdateCTFHUD(Players, cPlayer.Team);
                    UI.SendCTFFlagStates(cPlayer.Team, (ETeam)cPlayer.Team.TeamID, Players, EFlagState.RECOVERED);
                    Quest.CheckQuest(player, EQuestType.FLAGS_SAVED, questConditions);
                });

                DB.IncreasePlayerFlagsSaved(cPlayer.GamePlayer.SteamID, 1);
                DB.IncreasePlayerXP(cPlayer.GamePlayer.SteamID, Config.Medals.FileData.FlagSavedXP);
                return;
            }

            if (!cPlayer.IsCarryingFlag)
            {
                Logging.Debug($"{player.Player.CharacterName} is not carrying an enemy's flag");
                return;
            }

            Logging.Debug($"{player.Player.CharacterName} is carrying the enemy's flag, getting the flag, other team lost flag {otherTeam.HasFlag}");
            if (player.Player.Player.clothing.backpack == otherTeam.FlagID && !otherTeam.HasFlag)
            {
                player.Player.Player.clothing.thirdClothes.backpack = 0;
                player.Player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);

                ItemManager.dropItem(new(otherTeam.FlagID, true), otherTeam.FlagSP, true, true, true);
                Logging.Debug($"Spawning the other team's flag at {otherTeam.FlagSP} for location {Location.LocationName}");
                otherTeam.HasFlag = true;
                cPlayer.Team.Score++;
                cPlayer.Score += Config.Points.FileData.FlagCapturedPoints;
                cPlayer.XP += Config.Medals.FileData.FlagCapturedXP;
                cPlayer.FlagsCaptured++;

                UI.ShowXPUI(cPlayer.GamePlayer, Config.Medals.FileData.FlagCapturedXP, Plugin.Instance.Translate("Flag_Captured").ToRich());
                UI.SendFlagCapturedSound(cPlayer.Team.Players.Keys.Select(k => Plugin.Instance.Game.GetGamePlayer(k)).ToList());

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    UI.UpdateCTFHUD(Players, cPlayer.Team);
                    UI.UpdateCTFHUD(Players, otherTeam);
                    UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.CAPTURED);
                    Quest.CheckQuest(player, EQuestType.FLAGS_CAPTURED, questConditions);
                });

                if (cPlayer.Team.Score >= Config.CTF.FileData.ScoreLimit)
                    _ = Plugin.Instance.StartCoroutine(GameEnd(cPlayer.Team));

                DB.IncreasePlayerFlagsCaptured(cPlayer.GamePlayer.SteamID, 1);
                DB.IncreasePlayerXP(cPlayer.GamePlayer.SteamID, Config.Medals.FileData.FlagCapturedXP);
            }
            else
                Logging.Debug($"[ERROR] Could'nt find the other team's flag as the player's backpack");

            cPlayer.IsCarryingFlag = false;
        }
        else if (otherTeam.FlagID == itemData.item.id)
        {
            Logging.Debug($"{player.Player.CharacterName} is trying to pick up the other team's flag");

            if (otherTeam.HasFlag)
            {
                TaskDispatcher.QueueOnMainThread(() => UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.TAKEN));
                otherTeam.HasFlag = false;
            }
            else
                TaskDispatcher.QueueOnMainThread(() => UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.PICKED));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var ply = cPlayer.GamePlayer.Player.Player;
                if (cPlayer.GamePlayer.HasKillstreakActive)
                    cPlayer.GamePlayer.RemoveActiveKillstreak();
                else if (ply.equipment.equippedPage == 0)
                {
                    var secondary = ply.inventory.getItem(1, 0);
                    if (secondary != null)
                        ply.equipment.ServerEquip(1, secondary.x, secondary.y);
                    else
                        ply.equipment.dequip();
                }
            });

            cPlayer.IsCarryingFlag = true;
            UI.UpdateCTFHUD(Players, otherTeam);
        }
    }

    public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
    {
        var tPlayer = GetCTFPlayer(player.Player);
        if (tPlayer == null)
            return;

        if (text.Substring(0, 1) == "/")
            return;

        isVisible = false;
        TaskDispatcher.QueueOnMainThread(() =>
        {
            var data = player.Data;
            if (data.IsMuted)
            {
                var expiryTime = data.MuteExpiry.UtcDateTime - DateTime.UtcNow;
                Utility.Say(player.Player, $"<color=red>You are muted for{(expiryTime.Days == 0 ? "" : $" {expiryTime.Days} Days ")}{(expiryTime.Hours == 0 ? "" : $" {expiryTime.Hours} Hours")} {expiryTime.Minutes} Minutes. Reason: {data.MuteReason}</color>");
                return;
            }

            var iconLink = DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "";
            var updatedText =
                $"[{Utility.ToFriendlyName(chatMode)}] <color={Utility.GetLevelColor(player.Data.Level)}>[{player.Data.Level}]</color> <color={tPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={tPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

            var loopPlayers = chatMode == EChatMode.GLOBAL ? Players : Players.Where(k => k.Team == tPlayer.Team);
            foreach (var reciever in loopPlayers)
                //ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: iconLink, useRichTextFormatting: true);
                ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: player.Data.AvatarLinks[0], useRichTextFormatting: true);
        });
    }

    public override void OnVoiceChatUpdated(GamePlayer player)
    {
        if (GamePhase == EGamePhase.ENDING)
        {
            SendVoiceChat(Players.Select(k => k.GamePlayer).ToList(), false);
            return;
        }

        var tPlayer = GetCTFPlayer(player.Player);
        SendVoiceChat(Players.Where(k => k.Team == tPlayer.Team).Select(k => k.GamePlayer).ToList(), true);
    }

    public void GiveLoadout(CTFPlayer player)
    {
        player.GamePlayer.Player.Player.inventory.ClearInventory();
        Plugin.Instance.Loadout.GiveLoadout(player.GamePlayer);
    }

    public void SpawnPlayer(CTFPlayer player)
    {
        if (!SpawnPoints.TryGetValue(player.Team.SpawnPoint, out var spawnPoints))
            return;

        if (spawnPoints.Count == 0)
            return;

        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), spawnPoint.Yaw);
        player.GamePlayer.GiveSpawnProtection(Config.CTF.FileData.SpawnProtectionSeconds);
    }

    public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
    {
        var cPlayer = GetCTFPlayer(player.Player);
        if (cPlayer == null)
            return;

        if (throwable.equippedThrowableAsset.id == (player.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0))
            player.UsedLethal();
        else if (throwable.equippedThrowableAsset.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0))
            player.UsedTactical();
    }

    public override void PlayerConsumeableUsed(GamePlayer player, ItemConsumeableAsset consumeableAsset)
    {
        if (IsPlayerIngame(player.SteamID) && consumeableAsset.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0))
            player.UsedTactical();
    }

    public override void PlayerBarricadeSpawned(GamePlayer player, BarricadeDrop drop)
    {
        if (!IsPlayerIngame(player.SteamID))
            return;

        if (drop.asset.id == (player.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0))
        {
            player.UsedLethal();
            TaskDispatcher.QueueOnMainThread(() =>
            {
                BarricadeManager.BarricadeRegions.Cast<BarricadeRegion>().SelectMany(k => k.drops).Where(k => k != drop && k.asset.id == drop.asset.id && (k.GetServersideData()?.owner ?? 0UL) == player.SteamID.m_SteamID)
                    .Select(k => BarricadeManager.tryGetRegion(k.model.transform, out var x, out var y, out var plant, out var _) ? (k, x, y, plant) : (k, byte.MaxValue, byte.MaxValue, ushort.MaxValue)).ToList()
                    .ForEach(k => BarricadeManager.destroyBarricade(k.k, k.Item2, k.Item3, k.Item4));
            });
            
            return;
        }

        if (drop.asset.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0))
        {
            player.UsedTactical();
            return;
        }

        var turret = player.ActiveLoadout.Killstreaks.FirstOrDefault(k => k.Killstreak.KillstreakInfo.IsTurret && drop.asset.id == k.Killstreak.KillstreakInfo.TurretID);
        if (turret == null || drop.interactable is not InteractableSentry sentry)
            return;

        if (sentry.items.tryAddItem(new(turret.Killstreak.KillstreakInfo.GunID, true), true))
        {
            sentry.despawnWhenDestroyed = true;
            sentry.refreshDisplay();
        }

        GameTurrets.Add(player, (drop, turret.Killstreak.KillstreakInfo));
        GameTurretsInverse.Add(drop, player);
        GameTurretDamager.Add(drop, Plugin.Instance.StartCoroutine(DamageTurret(drop, turret.Killstreak.KillstreakInfo.TurretDamagePerSecond)));
    }

    public override void PlayerBarricadeDamaged(GamePlayer player, BarricadeDrop drop, ref ushort pendingTotalDamage, ref bool shouldAllow)
    {
        var damager = GetCTFPlayer(player.Player);
        if (damager == null)
            return;

        if (!GameTurretsInverse.TryGetValue(drop, out var gPlayer))
            return;

        var owner = GetCTFPlayer(gPlayer.Player);
        if (owner == null)
            return;

        if (owner.Team == damager.Team)
        {
            shouldAllow = false;
            return;
        }
        
        var barricadeData = drop.GetServersideData();
        if (barricadeData == null)
            return;

        if (barricadeData.barricade.health > pendingTotalDamage)
            return;

        UI.ShowXPUI(player, Config.Medals.FileData.TurretDestroyXP, Plugin.Instance.Translate("Turret_Destroy"));
        DB.IncreasePlayerXP(player.SteamID, Config.Medals.FileData.TurretDestroyXP);
    }

    public override void PlayerSendScoreboard(GamePlayer gPlayer, bool state)
    {
        var player = GetCTFPlayer(gPlayer.Player);
        if (player == null)
            return;

        if (state && !gPlayer.HasScoreboard)
        {
            if (gPlayer.ScoreboardCooldown > DateTime.UtcNow)
                return;
            
            gPlayer.HasScoreboard = true;
            var wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam : RedTeam.Score > BlueTeam.Score ? RedTeam : new(-1, true, new(), 0, Vector3.zero);
            UI.SetupCTFLeaderboard(player, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
            UI.ShowCTFLeaderboard(gPlayer);
        }
        else if (!state && gPlayer.HasScoreboard)
        {
            gPlayer.HasScoreboard = false;
            UI.HideCTFLeaderboard(gPlayer);
            gPlayer.ScoreboardCooldown = DateTime.UtcNow.AddSeconds(1);
        }
    }

    public override void PlayerStanceChanged(PlayerStance obj)
    {
        var cPlayer = GetCTFPlayer(obj.player);
        if (cPlayer == null)
            return;

        cPlayer.GamePlayer.OnStanceChanged(obj.stance);
    }
    
    public CTFPlayer GetCTFPlayer(CSteamID steamID) => PlayersLookup.TryGetValue(steamID, out var cPlayer) ? cPlayer : null;

    public CTFPlayer GetCTFPlayer(UnturnedPlayer player) => PlayersLookup.TryGetValue(player.CSteamID, out var cPlayer) ? cPlayer : null;

    public CTFPlayer GetCTFPlayer(Player player) => PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out var cPlayer) ? cPlayer : null;

    public override bool IsPlayerIngame(CSteamID steamID) => PlayersLookup.ContainsKey(steamID);

    public override int GetPlayerCount() => Players.Count;

    public override void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
    {
    }

    public override List<GamePlayer> GetPlayers() => Players.Select(k => k.GamePlayer).ToList();

    public override bool IsPlayerCarryingFlag(GamePlayer player)
    {
        var cPlayer = GetCTFPlayer(player.SteamID);
        return cPlayer != null && cPlayer.IsCarryingFlag;
    }

    public override TeamInfo GetTeam(GamePlayer player)
    {
        var cPlayer = GetCTFPlayer(player.SteamID);
        return cPlayer?.Team?.Info;
    }
}