﻿using Rocket.Core.Utils;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.TDM;

namespace UnturnedBlackout.GameTypes;

public class TDMGame : Game
{
    public Dictionary<int, List<TDMSpawnPoint>> SpawnPoints { get; set; }

    public List<TDMPlayer> Players { get; set; }
    public Dictionary<CSteamID, TDMPlayer> PlayersLookup { get; set; }

    public TDMTeam BlueTeam { get; set; }
    public TDMTeam RedTeam { get; set; }

    public Coroutine GameStarter { get; set; }
    public Coroutine GameEnder { get; set; }
    public Coroutine SpawnSwitcher { get; set; }

    public uint Frequency { get; set; }

    public TDMGame(ArenaLocation location, bool isHardcore) : base(EGameType.TDM, location, isHardcore)
    {
        SpawnPoints = new();
        foreach (var spawnPoint in Plugin.Instance.Data.Data.TDMSpawnPoints.Where(k => k.LocationID == location.LocationID))
        {
            if (SpawnPoints.TryGetValue(spawnPoint.GroupID, out var spawnPoints))
                spawnPoints.Add(spawnPoint);
            else
                SpawnPoints.Add(spawnPoint.GroupID, new() { spawnPoint });
        }

        Players = new();
        PlayersLookup = new();

        var blueTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
        var redTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

        BlueTeam = new(this, (byte)ETeam.BLUE, false, blueTeamInfo);
        RedTeam = new(this, (byte)ETeam.RED, false, redTeamInfo);
        Frequency = Utility.GetFreeFrequency();
    }

    public IEnumerator StartGame()
    {
        TaskDispatcher.QueueOnMainThread(() => CleanMap());
        GamePhase = EGamePhase.STARTING;
        foreach (var player in Players)
        {
            if (player.GamePlayer.IsLoading)
                continue;

            UI.ClearWaitingForPlayersUI(player.GamePlayer);
            player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
            UI.ShowCountdownUI(player.GamePlayer);
            SpawnPlayer(player);
        }

        for (var seconds = Config.TDM.FileData.StartSeconds; seconds >= 0; seconds--)
        {
            yield return new WaitForSeconds(1);

            foreach (var player in Players)
                UI.SendCountdownSeconds(player.GamePlayer, seconds);
        }

        GamePhase = EGamePhase.STARTED;

        foreach (var player in Players)
        {
            player.GamePlayer.GiveMovement(player.GamePlayer.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, false);
            player.StartTime = DateTime.UtcNow;
            UI.SendTDMHUD(player, BlueTeam, RedTeam);
            UI.ClearCountdownUI(player.GamePlayer);
        }

        SpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
        GameEnder = Plugin.Instance.StartCoroutine(EndGame());
    }

    public IEnumerator EndGame()
    {
        for (var seconds = Config.TDM.FileData.EndSeconds; seconds >= 0; seconds--)
        {
            yield return new WaitForSeconds(1);

            var timeSpan = TimeSpan.FromSeconds(seconds);
            foreach (var player in Players)
                UI.UpdateTDMTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
        }

        var wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam : RedTeam.Score > BlueTeam.Score ? RedTeam : new(this, -1, true, new());
        _ = Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
    }

    public IEnumerator GameEnd(TDMTeam wonTeam)
    {
        if (GameEnder != null)
            Plugin.Instance.StopCoroutine(GameEnder);

        GamePhase = EGamePhase.ENDING;
        UI.OnGameUpdated();

        var endTime = DateTime.UtcNow;
        List<GamePlayer> roundEndCasesPlayers = new();
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

        List<(GamePlayer, Case)> roundEndCases = new();
        foreach (var roundEndCasePlayer in roundEndCasesPlayers)
        {
            var @case = GetRandomRoundEndCase();
            if (@case == null)
                continue;

            roundEndCases.Add((roundEndCasePlayer, @case));
            DB.IncreasePlayerCase(roundEndCasePlayer.SteamID, @case.CaseID, 1);
        }

        Dictionary<GamePlayer, MatchEndSummary> summaries = new();
        foreach (var player in Players)
        {
            UI.ClearTDMHUD(player.GamePlayer);
            UI.ClearMidgameLoadoutUI(player.GamePlayer);
            if (player.GamePlayer.Player.Player.life.isDead)
                player.GamePlayer.Player.Player.life.ServerRespawn(false);

            UI.RemoveKillCard(player.GamePlayer);
            UI.ClearAnimations(player.GamePlayer);

            if (player.GamePlayer.HasScoreboard)
            {
                player.GamePlayer.HasScoreboard = false;
                UI.HideTDMLeaderboard(player.GamePlayer);
            }

            MatchEndSummary summary = new(player.GamePlayer, player.XP, player.StartingLevel, player.StartingXP, player.Kills, player.Deaths, player.Assists, player.HighestKillstreak, player.HighestMK, player.StartTime, GameMode, player.Team == wonTeam);
            summaries.Add(player.GamePlayer, summary);

            DB.IncreasePlayerXP(player.GamePlayer.SteamID, summary.PendingXP);
            DB.IncreasePlayerCredits(player.GamePlayer.SteamID, summary.PendingCredits);
            DB.IncreasePlayerBPXP(player.GamePlayer.SteamID, summary.BattlepassXP + summary.BattlepassBonusXP);

            TaskDispatcher.QueueOnMainThread(() => Quest.CheckQuest(player.GamePlayer, EQuestType.FINISH_MATCH, new() { { EQuestCondition.MAP, Location.LocationID }, { EQuestCondition.GAMEMODE, (int)GameMode }, { EQuestCondition.WIN_KILLS, player.Kills } }));
            if (player.Team == wonTeam)
                TaskDispatcher.QueueOnMainThread(() => Quest.CheckQuest(player.GamePlayer, EQuestType.WIN, new() { { EQuestCondition.MAP, Location.LocationID }, { EQuestCondition.GAMEMODE, (int)GameMode }, { EQuestCondition.WIN_KILLS, player.Kills } }));

            UI.SetupPreEndingUI(player.GamePlayer, EGameType.TDM, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName);
            player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
        }

        TaskDispatcher.QueueOnMainThread(() =>
        {
            UI.SetupTDMLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false, IsHardcore);
            CleanMap();
        });

        yield return new WaitForSeconds(5);

        foreach (var player in Players)
            UI.ShowTDMLeaderboard(player.GamePlayer);

        if (roundEndCases.Count > 0)
            _ = Plugin.Instance.StartCoroutine(UI.SetupRoundEndDrops(Players.Select(k => k.GamePlayer).ToList(), roundEndCases, 0));

        yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds);

        foreach (var player in Players.ToList())
        {
            RemovePlayerFromGame(player.GamePlayer);
            Plugin.Instance.Game.SendPlayerToLobby(player.GamePlayer.Player, summaries.TryGetValue(player.GamePlayer, out var pendingSummary) ? pendingSummary : null);
        }

        Players = new();
        BlueTeam.Destroy();
        RedTeam.Destroy();
        SpawnSwitcher.Stop();

        var locations = Plugin.Instance.Game.AvailableLocations;
        lock (locations)
        {
            var locString = "";
            foreach (var loc in locations)
            {
                var locc = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == loc);
                locString += $"{locc.LocationName},";
            }

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
        if (PlayersLookup.ContainsKey(player.SteamID))
            yield break;

        player.OnGameJoined(this);
        var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;
        TDMPlayer tPlayer = new(player, team);
        team.AddPlayer(player.SteamID);
        Players.Add(tPlayer);
        if (PlayersLookup.ContainsKey(player.SteamID))
            _ = PlayersLookup.Remove(player.SteamID);

        PlayersLookup.Add(player.SteamID, tPlayer);

        UI.OnGameCountUpdated(this);
        UI.SendLoadingUI(player.Player, true, GameMode, Location);
        for (var seconds = 1; seconds <= 5; seconds++)
        {
            yield return new WaitForSeconds(1);

            UI.UpdateLoadingBar(player.Player, new('　', Math.Min(96, seconds * 96 / 5)));
        }

        var currentPos = player.Player.Position;
        player.Player.Player.teleportToLocationUnsafe(new(currentPos.x, currentPos.y + 100, currentPos.z), 0);
        GiveLoadout(tPlayer);
        UI.SendPreEndingUI(tPlayer.GamePlayer);
        SpawnPlayer(tPlayer);
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
                    foreach (var ply in Players)
                    {
                        if (ply == tPlayer)
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
                TDMTeam wonTeam;
                wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam : RedTeam.Score > BlueTeam.Score ? RedTeam : new(this, -1, true, new());

                UI.SetupTDMLeaderboard(tPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
                UI.ShowTDMLeaderboard(tPlayer.GamePlayer);
                break;
            default:
                UI.SendTDMHUD(tPlayer, BlueTeam, RedTeam);
                break;
        }
    }

    public override void RemovePlayerFromGame(GamePlayer player)
    {
        if (!PlayersLookup.ContainsKey(player.SteamID))
            return;

        var tPlayer = GetTDMPlayer(player.Player);

        UI.ClearTDMHUD(player);
        UI.ClearPreEndingUI(player);
        UI.ClearVoiceChatUI(player);
        UI.ClearKillstreakUI(player);
        OnStoppedTalking(player);

        if (GamePhase == EGamePhase.STARTING)
        {
            UI.ClearCountdownUI(player);
            tPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
        }
        else if (GamePhase == EGamePhase.WAITING_FOR_PLAYERS)
        {
            UI.ClearWaitingForPlayersUI(player);
            foreach (var ply in Players)
            {
                if (ply == tPlayer)
                    continue;

                UI.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count - 1, Location.GetMinPlayers(GameMode));
            }
        }

        if (GamePhase != EGamePhase.ENDING)
            TaskDispatcher.QueueOnMainThread(() =>
                BarricadeManager.BarricadeRegions.Cast<BarricadeRegion>().SelectMany(k => k.drops).Where(k => (k.GetServersideData()?.owner ?? 0UL) == player.SteamID.m_SteamID && LevelNavigation.tryGetNavigation(k.model.transform.position, out var nav) && nav == Location.NavMesh)
                    .Select(k => BarricadeManager.tryGetRegion(k.model.transform, out var x, out var y, out var plant, out _) ? (k, x, y, plant) : (k, byte.MaxValue, byte.MaxValue, ushort.MaxValue)).ToList().ForEach(k => BarricadeManager.destroyBarricade(k.k, k.Item2, k.Item3, k.Item4)));

        tPlayer.Team.RemovePlayer(tPlayer.GamePlayer.SteamID);
        tPlayer.GamePlayer.OnGameLeft();
        _ = Players.Remove(tPlayer);
        _ = PlayersLookup.Remove(tPlayer.GamePlayer.SteamID);

        UI.OnGameCountUpdated(this);
    }

    public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
    {
        var tPlayer = GetTDMPlayer(player);
        if (tPlayer == null)
            return;

        if (cause == EDeathCause.SUICIDE)
        {
            RemovePlayerFromGame(tPlayer.GamePlayer);
            return;
        }

        if (tPlayer.GamePlayer.HasScoreboard)
        {
            tPlayer.GamePlayer.HasScoreboard = false;
            UI.HideTDMLeaderboard(tPlayer.GamePlayer);
        }

        var victimKS = tPlayer.Killstreak;
        var updatedKiller = cause == EDeathCause.WATER ? tPlayer.GamePlayer.SteamID : cause == EDeathCause.LANDMINE || cause == EDeathCause.SHRED ? tPlayer.GamePlayer.LastDamager.Count > 0 ? tPlayer.GamePlayer.LastDamager.Pop() : killer : killer;

        Logging.Debug($"Game player died, player name: {tPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");
        tPlayer.OnDeath(updatedKiller);
        tPlayer.GamePlayer.OnDeath(updatedKiller, Config.TDM.FileData.RespawnSeconds);
        tPlayer.Team.OnDeath(tPlayer.GamePlayer.SteamID);

        DB.IncreasePlayerDeaths(tPlayer.GamePlayer.SteamID, 1);

        TaskDispatcher.QueueOnMainThread(() =>
        {
            var kPlayer = GetTDMPlayer(updatedKiller);
            if (kPlayer == null)
            {
                Logging.Debug("Killer not found, returning");
                return;
            }

            if (kPlayer.GamePlayer.SteamID == tPlayer.GamePlayer.SteamID)
            {
                OnKill(kPlayer.GamePlayer, tPlayer.GamePlayer, cause == EDeathCause.WATER ? (ushort)0 : (ushort)1, kPlayer.Team.Info.KillFeedHexCode, tPlayer.Team.Info.KillFeedHexCode);

                Logging.Debug("Player killed themselves, returning");
                return;
            }

            Dictionary<EQuestCondition, int> questConditions = new() { { EQuestCondition.MAP, Location.LocationID }, { EQuestCondition.GAMEMODE, (int)GameMode } };

            Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");

            if (tPlayer.GamePlayer.LastDamager.Count > 0 && tPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                _ = tPlayer.GamePlayer.LastDamager.Pop();

            if (tPlayer.GamePlayer.LastDamager.Count > 0)
            {
                var assister = GetTDMPlayer(tPlayer.GamePlayer.LastDamager.Pop());
                if (assister != null && assister != kPlayer)
                {
                    assister.Assists++;
                    assister.Score += Config.Points.FileData.AssistPoints;
                    if (!assister.GamePlayer.Player.Player.life.isDead)
                        UI.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP, Plugin.Instance.Translate("Assist_Kill", tPlayer.GamePlayer.Player.CharacterName.ToUnrich()));

                    DB.IncreasePlayerXP(assister.GamePlayer.SteamID, Config.Medals.FileData.AssistKillXP);
                }

                tPlayer.GamePlayer.LastDamager.Clear();
            }

            var isFirstKill = Players[0].Kills == 0;
            kPlayer.Kills++;
            kPlayer.Team.Score++;
            kPlayer.Score += Config.Points.FileData.KillPoints;

            var xpGained = 0;
            var xpText = "";
            ushort equipmentUsed = 0;
            var longshotRange = 0f;

            var usedKillstreak = kPlayer.GamePlayer.HasKillstreakActive && (kPlayer.GamePlayer.ActiveKillstreak?.Killstreak?.KillstreakInfo?.IsItem ?? false) && cause != EDeathCause.SENTRY;
            var killstreakID = kPlayer.GamePlayer.ActiveKillstreak?.Killstreak?.KillstreakID ?? 0;

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
                    default:
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

            if (kPlayer.PlayersKilled.ContainsKey(tPlayer.GamePlayer.SteamID))
            {
                kPlayer.PlayersKilled[tPlayer.GamePlayer.SteamID] += 1;
                if (kPlayer.PlayersKilled[tPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                {
                    xpGained += Config.Medals.FileData.DominationXP;
                    xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                    Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.DOMINATION, questConditions);
                }
            }
            else
                kPlayer.PlayersKilled.Add(tPlayer.GamePlayer.SteamID, 1);

            if (tPlayer.GamePlayer.SteamID == kPlayer.GamePlayer.LastKiller)
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

            if (!usedKillstreak && cause == EDeathCause.GUN && (tPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
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
            kPlayer.GamePlayer.OnKilled(tPlayer.GamePlayer);

            foreach (var ply in Players)
                UI.UpdateTDMScore(ply, kPlayer.Team);

            if (kPlayer.Team.Score == Config.TDM.FileData.ScoreLimit)
                _ = Plugin.Instance.StartCoroutine(GameEnd(kPlayer.Team));

            if (equipmentUsed != 0)
                OnKill(kPlayer.GamePlayer, tPlayer.GamePlayer, equipmentUsed, kPlayer.Team.Info.KillFeedHexCode, tPlayer.Team.Info.KillFeedHexCode);

            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.KILL, questConditions);
            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.MULTI_KILL, questConditions);
            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.KILLSTREAK, questConditions);
            if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.HEADSHOTS, questConditions);

            Quest.CheckQuest(tPlayer.GamePlayer, EQuestType.DEATH, questConditions);

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
        var player = GetTDMPlayer(parameters.player);
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

        var kPlayer = GetTDMPlayer(parameters.killer);
        if (kPlayer == null)
            return;

        if (kPlayer.Team == player.Team && kPlayer != player)
        {
            shouldAllow = false;
            return;
        }

        parameters.damage += (kPlayer.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageIncreasePerkName, out var damageIncreaserPerk) ? (float)damageIncreaserPerk.Perk.SkillLevel / 100 : 0f) * parameters.damage;

        if (parameters.cause == EDeathCause.GRENADE && parameters.damage < player.GamePlayer.Player.Player.life.health)
            UI.ShowXPUI(kPlayer.GamePlayer, Config.Medals.FileData.LethalHitXP, Plugin.Instance.Translate("Lethal_Hit"));

        if (kPlayer.GamePlayer.HasSpawnProtection)
        {
            kPlayer.GamePlayer.SpawnProtectionRemover.Stop();
            kPlayer.GamePlayer.HasSpawnProtection = false;
        }
    }

    public override void OnPlayerRevived(UnturnedPlayer player)
    {
        var tPlayer = GetTDMPlayer(player);
        if (tPlayer == null)
            return;

        tPlayer.GamePlayer.OnRevived();
    }

    public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition, ref float yaw)
    {
        var tPlayer = GetTDMPlayer(player.Player);
        if (tPlayer == null)
            return;

        if (!SpawnPoints.TryGetValue(tPlayer.Team.SpawnPoint, out var spawnPoints))
            return;

        if (spawnPoints.Count == 0)
            return;

        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        respawnPosition = spawnPoint.GetSpawnPoint();
        yaw = spawnPoint.Yaw;

        player.GiveSpawnProtection(Config.TDM.FileData.SpawnProtectionSeconds);
    }

    public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
    {
        var tPlayer = GetTDMPlayer(player.Player);
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
                Utility.Say(player.Player, $"<color=red>You are muted for{(expiryTime.Days == 0 ? "" : $" {expiryTime.Days} Days ")}{(expiryTime.Hours == 0 ? "" : $" {expiryTime.Hours} Hours")} {expiryTime.Minutes} Minutes");
                return;
            }

            var iconLink = DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "";
            var updatedText =
                $"[{Utility.ToFriendlyName(chatMode)}] <color={Utility.GetLevelColor(player.Data.Level)}>[{player.Data.Level}]</color> <color={tPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={tPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

            var loopPlayers = chatMode == EChatMode.GLOBAL ? Players : Players.Where(k => k.Team == tPlayer.Team);
            foreach (var reciever in loopPlayers)
                ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: player.Data.AvatarLink, useRichTextFormatting: true);
        });
    }

    public override void OnVoiceChatUpdated(GamePlayer player)
    {
        if (GamePhase == EGamePhase.ENDING)
        {
            SendVoiceChat(Players.Select(k => k.GamePlayer).ToList(), false);
            return;
        }

        var tPlayer = GetTDMPlayer(player.Player);
        SendVoiceChat(Players.Where(k => k.Team == tPlayer.Team).Select(k => k.GamePlayer).ToList(), true);
    }

    public void GiveLoadout(TDMPlayer player)
    {
        player.GamePlayer.Player.Player.inventory.ClearInventory();
        Plugin.Instance.Loadout.GiveLoadout(player.GamePlayer);
    }

    public void SpawnPlayer(TDMPlayer player)
    {
        if (!SpawnPoints.TryGetValue(player.Team.SpawnPoint, out var spawnPoints))
            return;

        if (spawnPoints.Count == 0)
            return;

        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), spawnPoint.Yaw);
        player.GamePlayer.GiveSpawnProtection(Config.TDM.FileData.SpawnProtectionSeconds);
    }

    public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
    {
        var tPlayer = GetTDMPlayer(player.Player);
        if (tPlayer == null)
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
            sentry.refreshDisplay();
            sentry.despawnWhenDestroyed = true;
        }

        GameTurrets.Add(player, (drop, turret.Killstreak.KillstreakInfo));
        GameTurretsInverse.Add(drop, player);
        GameTurretDamager.Add(drop, Plugin.Instance.StartCoroutine(DamageTurret(drop, turret.Killstreak.KillstreakInfo.TurretDamagePerSecond)));
    }

    public override void PlayerBarricadeDamaged(GamePlayer player, BarricadeDrop drop, ref ushort pendingTotalDamage, ref bool shouldAllow)
    {
        var damager = GetTDMPlayer(player.Player);
        if (damager == null)
            return;

        if (!GameTurretsInverse.TryGetValue(drop, out var gPlayer))
            return;

        var owner = GetTDMPlayer(gPlayer.Player);
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

    public override void PlayerChangeFiremode(GamePlayer player)
    {
        var tPlayer = GetTDMPlayer(player.Player);
        if (tPlayer == null)
            return;

        if (GamePhase == EGamePhase.ENDING)
            return;

        if (player.ScoreboardCooldown > DateTime.UtcNow)
            return;

        player.ScoreboardCooldown = DateTime.UtcNow.AddSeconds(0.5);

        if (tPlayer.GamePlayer.HasScoreboard)
        {
            tPlayer.GamePlayer.HasScoreboard = false;
            UI.HideTDMLeaderboard(tPlayer.GamePlayer);
        }
        else
        {
            tPlayer.GamePlayer.HasScoreboard = true;
            var wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam : RedTeam.Score > BlueTeam.Score ? RedTeam : new(this, -1, true, new());
            UI.SetupTDMLeaderboard(tPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
            UI.ShowTDMLeaderboard(tPlayer.GamePlayer);
        }
    }

    public override void PlayerStanceChanged(PlayerStance obj)
    {
        var tPlayer = GetTDMPlayer(obj.player);
        if (tPlayer == null)
            return;

        tPlayer.GamePlayer.OnStanceChanged(obj.stance);
    }

    public override void PlayerEquipmentChanged(GamePlayer player)
    {
        if (IsPlayerIngame(player.SteamID) && GamePhase != EGamePhase.STARTING)
            player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, true);
    }

    public override void PlayerAimingChanged(GamePlayer player, bool isAiming)
    {
        if (IsPlayerIngame(player.SteamID) && GamePhase != EGamePhase.STARTING)
            player.GiveMovement(isAiming, false, false);
    }

    public IEnumerator SpawnSwitch()
    {
        yield return new WaitForSeconds(Config.Base.FileData.SpawnSwitchSeconds);

        SwitchSpawn();
    }

    public void SwitchSpawn()
    {
        SpawnSwitcher.Stop();
        RedTeam.CheckSpawnSwitcher.Stop();
        BlueTeam.CheckSpawnSwitcher.Stop();

        var keys = SpawnPoints.Keys.ToList();
        if (keys.Count == 0)
            return;

        var currentSpawn = (BlueTeam.SpawnPoint, RedTeam.SpawnPoint);
        var forwardPossibleSpawn = (BlueTeam.SpawnPoint + 2, RedTeam.SpawnPoint + 2); // If blue has 0 and red has 1, the next possible group is 2 and 3
        var backwardPossibleSpawn = (BlueTeam.SpawnPoint - 2, RedTeam.SpawnPoint - 2); // If blue has 2 and 3, the backward possible group is 0 and 1

        var shouldSwitch = UnityEngine.Random.Range(1, 101) > 50;
        // check if forward is possible
        if (keys.Contains(forwardPossibleSpawn.Item1) && keys.Contains(forwardPossibleSpawn.Item2))
        {
            BlueTeam.SpawnPoint = shouldSwitch ? forwardPossibleSpawn.Item1 : forwardPossibleSpawn.Item2;
            RedTeam.SpawnPoint = shouldSwitch ? forwardPossibleSpawn.Item2 : forwardPossibleSpawn.Item1;
        } // Check if backward is possible
        else if (keys.Contains(backwardPossibleSpawn.Item1) && keys.Contains(backwardPossibleSpawn.Item2))
        {
            BlueTeam.SpawnPoint = shouldSwitch ? backwardPossibleSpawn.Item1 : backwardPossibleSpawn.Item2;
            RedTeam.SpawnPoint = shouldSwitch ? backwardPossibleSpawn.Item2 : backwardPossibleSpawn.Item1;
        } // If all else fails, switch the current spawn
        else
        {
            BlueTeam.SpawnPoint = currentSpawn.Item2;
            RedTeam.SpawnPoint = currentSpawn.Item1;
        }

        SpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
    }

    public TDMPlayer GetTDMPlayer(CSteamID steamID) => PlayersLookup.TryGetValue(steamID, out var tPlayer) ? tPlayer : null;

    public TDMPlayer GetTDMPlayer(UnturnedPlayer player) => PlayersLookup.TryGetValue(player.CSteamID, out var tPlayer) ? tPlayer : null;

    public TDMPlayer GetTDMPlayer(Player player) => PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out var tPlayer) ? tPlayer : null;

    public override bool IsPlayerIngame(CSteamID steamID) => PlayersLookup.ContainsKey(steamID);

    public override int GetPlayerCount() => Players.Count;

    public override void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
    {
    }

    public override List<GamePlayer> GetPlayers() => Players.Select(k => k.GamePlayer).ToList();

    public override void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow)
    {
    }

    public override bool IsPlayerCarryingFlag(GamePlayer player) => false;

    public override TeamInfo GetTeam(GamePlayer player)
    {
        var tPlayer = GetTDMPlayer(player.SteamID);
        return tPlayer?.Team?.Info;
    }
}