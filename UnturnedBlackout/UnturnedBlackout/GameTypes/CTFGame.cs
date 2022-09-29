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
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.Global;

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

        foreach (var spawnPoint in Plugin.Instance.Data.Data.CTFSpawnPoints.Where(k =>
                     k.LocationID == location.LocationID))
        {
            if (spawnPoint.IsFlagSP)
            {
                if ((ETeam)spawnPoint.GroupID == ETeam.Blue)
                    blueFlag = spawnPoint.GetSpawnPoint();
                else if ((ETeam)spawnPoint.GroupID == ETeam.Red) redFlag = spawnPoint.GetSpawnPoint();

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

        BlueTeam = new((byte)ETeam.Blue, false, blueTeamInfo, Config.CTF.FileData.BlueFlagID, blueFlag);
        RedTeam = new((byte)ETeam.Red, false, redTeamInfo, Config.CTF.FileData.RedFlagID, redFlag);
        Frequency = Utility.GetFreeFrequency();
    }

    public IEnumerator StartGame()
    {
        GamePhase = EGamePhase.Starting;
        foreach (var player in Players)
        {
            if (player.GamePlayer.IsLoading) continue;
            Plugin.Instance.UI.ClearWaitingForPlayersUI(player.GamePlayer);
            player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
            Plugin.Instance.UI.ShowCountdownUI(player.GamePlayer);
            SpawnPlayer(player);
        }

        for (var seconds = Config.CTF.FileData.StartSeconds; seconds >= 0; seconds--)
        {
            yield return new WaitForSeconds(1);
            foreach (var player in Players) Plugin.Instance.UI.SendCountdownSeconds(player.GamePlayer, seconds);
        }

        GamePhase = EGamePhase.Started;
        foreach (var player in Players)
        {
            player.GamePlayer.GiveMovement(
                player.GamePlayer.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, false);
            player.StartTime = DateTime.UtcNow;
            Plugin.Instance.UI.ClearCountdownUI(player.GamePlayer);
        }

        Plugin.Instance.UI.SendCTFHUD(BlueTeam, RedTeam, Players);

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
                Plugin.Instance.UI.UpdateCTFTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
        }

        var wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam :
            RedTeam.Score > BlueTeam.Score ? RedTeam : new(-1, true, new(), 0, Vector3.zero);
        _ = Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
    }

    public IEnumerator GameEnd(CTFTeam wonTeam)
    {
        if (GameEnder != null) Plugin.Instance.StopCoroutine(GameEnder);

        GamePhase = EGamePhase.Ending;
        Plugin.Instance.UI.OnGameUpdated();

        var endTime = DateTime.UtcNow;
        List<GamePlayer> roundEndCasesPlayers = new();
        foreach (var player in Players)
        {
            var totalMinutesPlayed = (int)(endTime - player.StartTime).TotalMinutes;
            if (totalMinutesPlayed < Config.RoundEndCases.FileData.MinimumMinutesPlayed || player.Kills == 0) continue;

            var chance = Config.RoundEndCases.FileData.Chance * totalMinutesPlayed;
            if (UnityEngine.Random.Range(1, 101) > chance) continue;

            roundEndCasesPlayers.Add(player.GamePlayer);
            if (roundEndCasesPlayers.Count == 8) break;
        }

        List<(GamePlayer, Case)> roundEndCases = new();
        foreach (var roundEndCasePlayer in roundEndCasesPlayers)
        {
            var @case = GetRandomRoundEndCase();
            if (@case == null) continue;

            roundEndCases.Add((roundEndCasePlayer, @case));
            _ = Task.Run(async () =>
                await Plugin.Instance.DB.IncreasePlayerCaseAsync(roundEndCasePlayer.SteamID, @case.CaseID, 1));
        }

        Dictionary<GamePlayer, MatchEndSummary> summaries = new();
        foreach (var player in Players)
        {
            Plugin.Instance.UI.ClearCTFHUD(player.GamePlayer);
            Plugin.Instance.UI.ClearMidgameLoadoutUI(player.GamePlayer);
            if (player.GamePlayer.Player.Player.life.isDead) player.GamePlayer.Player.Player.life.ServerRespawn(false);

            Plugin.Instance.UI.RemoveKillCard(player.GamePlayer);
            Plugin.Instance.UI.ClearAnimations(player.GamePlayer);

            if (player.GamePlayer.HasScoreboard)
            {
                player.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UI.HideCTFLeaderboard(player.GamePlayer);
            }

            MatchEndSummary summary = new(player.GamePlayer, player.XP, player.StartingLevel, player.StartingXP,
                player.Kills, player.Deaths, player.Assists, player.HighestKillstreak, player.HighestMK,
                player.StartTime, GameMode, player.Team == wonTeam);
            summaries.Add(player.GamePlayer, summary);
            _ = Task.Run(async () =>
            {
                await Plugin.Instance.DB.IncreasePlayerXPAsync(player.GamePlayer.SteamID, summary.PendingXP);
                await Plugin.Instance.DB.IncreasePlayerCreditsAsync(player.GamePlayer.SteamID, summary.PendingCredits);
                await Plugin.Instance.DB.IncreasePlayerBPXPAsync(player.GamePlayer.SteamID,
                    summary.BattlepassXP + summary.BattlepassBonusXP);
            });

            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(player.GamePlayer,
                EQuestType.FinishMatch,
                new()
                {
                    { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode },
                    { EQuestCondition.WinFlagsCaptured, player.FlagsCaptured },
                    { EQuestCondition.WinFlagsSaved, player.FlagsSaved }, { EQuestCondition.WinKills, player.Kills }
                }));
            if (player.Team == wonTeam)
                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(player.GamePlayer,
                    EQuestType.Win,
                    new()
                    {
                        { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode },
                        { EQuestCondition.WinFlagsCaptured, player.FlagsCaptured },
                        { EQuestCondition.WinFlagsSaved, player.FlagsSaved }, { EQuestCondition.WinKills, player.Kills }
                    }));

            Plugin.Instance.UI.SetupPreEndingUI(player.GamePlayer, EGameType.CTF, player.Team.TeamID == wonTeam.TeamID,
                BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName);
            player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
        }

        TaskDispatcher.QueueOnMainThread(() =>
        {
            Plugin.Instance.UI.SetupCTFLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false, IsHardcore);
            CleanMap();
        });
        yield return new WaitForSeconds(5);
        foreach (var player in Players) Plugin.Instance.UI.ShowCTFLeaderboard(player.GamePlayer);

        if (roundEndCases.Count > 0)
            _ = Plugin.Instance.StartCoroutine(
                Plugin.Instance.UI.SetupRoundEndDrops(Players.Select(k => k.GamePlayer).ToList(), roundEndCases, 2));

        yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds);
        foreach (var player in Players.ToList())
        {
            RemovePlayerFromGame(player.GamePlayer);
            Plugin.Instance.Game.SendPlayerToLobby(player.GamePlayer.Player,
                summaries.TryGetValue(player.GamePlayer, out var pendingSummary) ? pendingSummary : null);
        }

        Players = new();
        BlueTeam.Destroy();
        RedTeam.Destroy();

        var locations = Plugin.Instance.Game.AvailableLocations;
        lock (locations)
        {
            var locString = "";
            foreach (var loc in locations)
            {
                var locc = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == loc);
                locString += $"{locc.LocationName},";
            }

            var randomLocation = locations.Count > 0
                ? locations[UnityEngine.Random.Range(0, locations.Count)]
                : Location.LocationID;
            var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == randomLocation);
            var gameMode = Plugin.Instance.Game.GetRandomGameMode(location.LocationID);
            GamePhase = EGamePhase.Ended;
            Plugin.Instance.Game.EndGame(this);
            Plugin.Instance.Game.StartGame(location, gameMode.Item1, gameMode.Item2);
        }
    }

    public override IEnumerator AddPlayerToGame(GamePlayer player)
    {
        if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID)) yield break;

        player.OnGameJoined(this);
        var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;
        CTFPlayer cPlayer = new(player, team);
        team.AddPlayer(player.SteamID);
        Players.Add(cPlayer);
        if (PlayersLookup.ContainsKey(player.SteamID)) _ = PlayersLookup.Remove(player.SteamID);

        PlayersLookup.Add(player.SteamID, cPlayer);

        Plugin.Instance.UI.OnGameCountUpdated(this);
        Plugin.Instance.UI.SendLoadingUI(player.Player, true, GameMode, Location);
        for (var seconds = 1; seconds <= 5; seconds++)
        {
            yield return new WaitForSeconds(1);
            Plugin.Instance.UI.UpdateLoadingBar(player.Player, new('　', Math.Min(96, seconds * 96 / 5)));
        }

        var currentPos = player.Player.Position;
        player.Player.Player.teleportToLocationUnsafe(new(currentPos.x, currentPos.y + 100, currentPos.z), 0);
        GiveLoadout(cPlayer);
        Plugin.Instance.UI.SendPreEndingUI(cPlayer.GamePlayer);
        SpawnPlayer(cPlayer);
        Plugin.Instance.UI.ClearLoadingUI(player.Player);
        Plugin.Instance.UI.SendVoiceChatUI(player);

        player.IsLoading = false;
        switch (GamePhase)
        {
            case EGamePhase.WaitingForPlayers:
                var minPlayers = Location.GetMinPlayers(GameMode);
                if (Players.Count >= minPlayers)
                    GameStarter = Plugin.Instance.StartCoroutine(StartGame());
                else
                {
                    Plugin.Instance.UI.SendWaitingForPlayersUI(player, Players.Count, minPlayers);
                    foreach (var ply in Players)
                    {
                        if (ply == cPlayer) continue;

                        Plugin.Instance.UI.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
                    }
                }

                break;
            case EGamePhase.Starting:
                player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UI.ShowCountdownUI(player);
                break;
            case EGamePhase.Ending:
                CTFTeam wonTeam;
                wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam :
                    RedTeam.Score > BlueTeam.Score ? RedTeam : new(-1, true, new(), 0, Vector3.zero);

                Plugin.Instance.UI.SetupCTFLeaderboard(cPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true,
                    IsHardcore);
                Plugin.Instance.UI.ShowCTFLeaderboard(cPlayer.GamePlayer);
                break;
            default:
                Plugin.Instance.UI.SendCTFHUD(cPlayer, BlueTeam, RedTeam, Players);
                break;
        }
    }

    public override void RemovePlayerFromGame(GamePlayer player)
    {
        if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID)) return;

        var cPlayer = GetCTFPlayer(player.Player);

        Plugin.Instance.UI.ClearCTFHUD(player);
        Plugin.Instance.UI.ClearPreEndingUI(player);
        Plugin.Instance.UI.ClearVoiceChatUI(player);
        Plugin.Instance.UI.ClearKillstreakUI(player);

        OnStoppedTalking(player);

        if (GamePhase == EGamePhase.Starting)
        {
            Plugin.Instance.UI.ClearCountdownUI(player);
            cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
        }
        else if (GamePhase == EGamePhase.WaitingForPlayers) Plugin.Instance.UI.ClearWaitingForPlayersUI(player);

        if (GamePhase != EGamePhase.Ending)
            TaskDispatcher.QueueOnMainThread(() =>
                BarricadeManager.BarricadeRegions.Cast<BarricadeRegion>().SelectMany(k => k.drops)
                    .Where(k => (k.GetServersideData()?.owner ?? 0UL) == player.SteamID.m_SteamID &&
                                LevelNavigation.tryGetNavigation(k.model.transform.position, out var nav) &&
                                nav == Location.NavMesh)
                    .Select(k =>
                        BarricadeManager.tryGetRegion(k.model.transform, out var x, out var y, out var plant, out _)
                            ? (k, x, y, plant)
                            : (k, byte.MaxValue, byte.MaxValue, ushort.MaxValue)).ToList().ForEach(k =>
                        BarricadeManager.destroyBarricade(k.k, k.Item2, k.Item3, k.Item4)));

        if (cPlayer != null)
        {
            cPlayer.Team.RemovePlayer(cPlayer.GamePlayer.SteamID);
            cPlayer.GamePlayer.OnGameLeft();
            _ = Players.Remove(cPlayer);
            _ = PlayersLookup.Remove(cPlayer.GamePlayer.SteamID);

            if (cPlayer.IsCarryingFlag)
            {
                var otherTeam = cPlayer.Team.TeamID == BlueTeam.TeamID ? RedTeam : BlueTeam;
                if (cPlayer.GamePlayer.Player.Player.clothing.backpack == otherTeam.FlagID)
                    ItemManager.dropItem(new(otherTeam.FlagID, true),
                        cPlayer.GamePlayer.Player.Player.transform.position, true, true, true);

                cPlayer.IsCarryingFlag = false;
                cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1f);
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    Plugin.Instance.UI.UpdateCTFHUD(Players, otherTeam);
                    Plugin.Instance.UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players,
                        EFlagState.Dropped);
                });
            }
        }

        Plugin.Instance.UI.OnGameCountUpdated(this);
    }

    public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
    {
        var cPlayer = GetCTFPlayer(player);
        if (cPlayer == null) return;

        if (cause == EDeathCause.SUICIDE)
        {
            RemovePlayerFromGame(cPlayer.GamePlayer);
            return;
        }

        if (cPlayer.GamePlayer.HasScoreboard)
        {
            cPlayer.GamePlayer.HasScoreboard = false;
            Plugin.Instance.UI.HideCTFLeaderboard(cPlayer.GamePlayer);
        }

        var victimKS = cPlayer.Killstreak;
        Logging.Debug($"Game player died, player name: {cPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");

        var updatedKiller = cause == EDeathCause.WATER
            ? cPlayer.GamePlayer.SteamID
            :
            cause is EDeathCause.LANDMINE or EDeathCause.SHRED
                ?
                cPlayer.GamePlayer.LastDamager.Count > 0 ? cPlayer.GamePlayer.LastDamager.Pop() : killer
                : killer;

        cPlayer.OnDeath(updatedKiller);
        cPlayer.GamePlayer.OnDeath(updatedKiller, Config.CTF.FileData.RespawnSeconds);

        var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;
        var isFlagCarrier = false;

        if (cPlayer.IsCarryingFlag)
        {
            isFlagCarrier = true;
            if (player.clothing.backpack == otherTeam.FlagID)
                ItemManager.dropItem(new(otherTeam.FlagID, true),
                    cause == EDeathCause.WATER ? otherTeam.FlagSP : player.transform.position, true, true, true);

            cPlayer.IsCarryingFlag = false;
            cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1f);
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UI.UpdateCTFHUD(Players, otherTeam);
                Plugin.Instance.UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players,
                    EFlagState.Dropped);
            });
        }

        _ = Task.Run(async () => await Plugin.Instance.DB.IncreasePlayerDeathsAsync(cPlayer.GamePlayer.SteamID, 1));

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
                OnKill(kPlayer.GamePlayer, cPlayer.GamePlayer, cause == EDeathCause.WATER ? (ushort)0 : (ushort)1,
                    kPlayer.Team.Info.KillFeedHexCode, cPlayer.Team.Info.KillFeedHexCode);

                Logging.Debug("Player killed themselves, returning");
                return;
            }

            Dictionary<EQuestCondition, int> questConditions = new()
            {
                { EQuestCondition.Map, Location.LocationID },
                { EQuestCondition.Gamemode, (int)GameMode }
            };

            Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");

            if (cPlayer.GamePlayer.LastDamager.Count > 0 &&
                cPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                _ = cPlayer.GamePlayer.LastDamager.Pop();

            if (cPlayer.GamePlayer.LastDamager.Count > 0)
            {
                var assister = GetCTFPlayer(cPlayer.GamePlayer.LastDamager.Pop());
                if (assister != null && assister != kPlayer)
                {
                    assister.Assists++;
                    assister.Score += Config.Points.FileData.AssistPoints;
                    if (!assister.GamePlayer.Player.Player.life.isDead)
                        Plugin.Instance.UI.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP,
                            Plugin.Instance.Translate("Assist_Kill",
                                cPlayer.GamePlayer.Player.CharacterName.ToUnrich()));

                    _ = Task.Run(async () =>
                        await Plugin.Instance.DB.IncreasePlayerXPAsync(assister.GamePlayer.SteamID,
                            Config.Medals.FileData.AssistKillXP));
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

            var usedKillstreak = kPlayer.GamePlayer.HasKillstreakActive &&
                                 (kPlayer.GamePlayer.ActiveKillstreak?.Killstreak?.KillstreakInfo?.IsItem ?? false) &&
                                 cause != EDeathCause.SENTRY;
            var killstreakID = kPlayer.GamePlayer.ActiveKillstreak?.Killstreak?.KillstreakID ?? 0;

            if (usedKillstreak)
            {
                var info = kPlayer.GamePlayer.ActiveKillstreak.Killstreak.KillstreakInfo;
                xpGained += info.MedalXP;
                xpText += info.MedalName;
                equipmentUsed += info.ItemID;
                questConditions.Add(EQuestCondition.Killstreak, killstreakID);
            }
            else
            {
                switch (cause)
                {
                    case EDeathCause.MELEE:
                        xpGained += Config.Medals.FileData.MeleeKillXP;
                        xpText += Plugin.Instance.Translate("Melee_Kill").ToRich();
                        equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Knife?.Knife?.KnifeID ?? 0;
                        questConditions.Add(EQuestCondition.Knife, equipmentUsed);
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
                        if (equipment == (kPlayer.GamePlayer.ActiveLoadout.PrimarySkin?.SkinID ?? 0) ||
                            equipment == (kPlayer.GamePlayer.ActiveLoadout.Primary?.Gun?.GunID ?? 0))
                        {
                            questConditions.Add(EQuestCondition.GunType,
                                (int)kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunType);
                            equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunID;
                            longshotRange = kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.LongshotRange;
                        }
                        else if (equipment == (kPlayer.GamePlayer.ActiveLoadout.SecondarySkin?.SkinID ?? 0) ||
                                 equipment == (kPlayer.GamePlayer.ActiveLoadout.Secondary?.Gun?.GunID ?? 0))
                        {
                            questConditions.Add(EQuestCondition.GunType,
                                (int)kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunType);
                            equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunID;
                            longshotRange = kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.LongshotRange;
                        }
                        else
                            equipmentUsed = equipment;

                        questConditions.Add(EQuestCondition.Gun, equipmentUsed);
                        break;
                    case EDeathCause.CHARGE:
                    case EDeathCause.GRENADE:
                    case EDeathCause.LANDMINE:
                    case EDeathCause.BURNING:
                    case EDeathCause.SHRED:
                        xpGained += Config.Medals.FileData.LethalKillXP;
                        xpText += Plugin.Instance.Translate("Lethal_Kill").ToRich();
                        equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0;
                        questConditions.Add(EQuestCondition.Gadget, equipmentUsed);
                        break;
                    case EDeathCause.SENTRY:
                        if (!GameTurrets.TryGetValue(kPlayer.GamePlayer, out var sentry)) break;

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
            questConditions.Add(EQuestCondition.TargetKS, kPlayer.Killstreak);
            if (kPlayer.MultipleKills == 0)
                kPlayer.SetMultipleKills(kPlayer.MultipleKills + 1);
            else if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
            {
                kPlayer.SetMultipleKills(kPlayer.MultipleKills + 1);
                xpGained += Config.Medals.FileData.BaseXPMK +
                            kPlayer.MultipleKills * Config.Medals.FileData.IncreaseXPPerMK;
                var multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
                xpText += (multiKillText == $"Multiple_Kills_Show_{kPlayer.MultipleKills}"
                    ? Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich()
                    : multiKillText) + "\n";
            }
            else
                kPlayer.SetMultipleKills(1);

            questConditions.Add(EQuestCondition.TargetMK, kPlayer.MultipleKills);

            if (victimKS > Config.Medals.FileData.ShutdownKillStreak)
            {
                xpGained += Config.Medals.FileData.ShutdownXP;
                xpText += Plugin.Instance.Translate("Shutdown_Kill").ToRich() + "\n";
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Shutdown, questConditions);
            }

            if (kPlayer.PlayersKilled.ContainsKey(cPlayer.GamePlayer.SteamID))
            {
                kPlayer.PlayersKilled[cPlayer.GamePlayer.SteamID] += 1;
                if (kPlayer.PlayersKilled[cPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                {
                    xpGained += Config.Medals.FileData.DominationXP;
                    xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                    Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Domination, questConditions);
                }
            }
            else
                kPlayer.PlayersKilled.Add(cPlayer.GamePlayer.SteamID, 1);

            if (isFlagCarrier)
            {
                xpGained += Config.Medals.FileData.FlagCarrierKilledXP;
                xpText += Plugin.Instance.Translate("Flag_Killer").ToRich() + "\n";
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.FlagKiller, questConditions);
            }

            if (kPlayer.IsCarryingFlag)
            {
                xpGained += Config.Medals.FileData.KillWhileCarryingFlagXP;
                xpText += Plugin.Instance.Translate("Flag_Denied").ToRich() + "\n";
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.FlagDenied, questConditions);
            }

            if (cPlayer.GamePlayer.SteamID == kPlayer.GamePlayer.LastKiller)
            {
                xpGained += Config.Medals.FileData.RevengeXP;
                xpText += Plugin.Instance.Translate("Revenge_Kill").ToRich() + "\n";
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Revenge, questConditions);
            }

            if (isFirstKill)
            {
                xpGained += Config.Medals.FileData.FirstKillXP;
                xpText += Plugin.Instance.Translate("First_Kill").ToRich() + "\n";
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.FirstKill, questConditions);
            }

            if (!usedKillstreak && cause == EDeathCause.GUN &&
                (cPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
            {
                xpGained += Config.Medals.FileData.LongshotXP;
                xpText += Plugin.Instance.Translate("Longshot_Kill").ToRich() + "\n";
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Longshot, questConditions);
            }

            if (kPlayer.GamePlayer.Player.Player.life.health < Config.Medals.FileData.HealthSurvivorKill)
            {
                xpGained += Config.Medals.FileData.SurvivorXP;
                xpText += Plugin.Instance.Translate("Survivor_Kill").ToRich() + "\n";
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Survivor, questConditions);
            }

            kPlayer.GamePlayer.LastKiller = CSteamID.Nil;
            kPlayer.LastKill = DateTime.UtcNow;
            kPlayer.XP += xpGained;
            Players.Sort((x, y) => y.Kills.CompareTo(x.Kills));

            Plugin.Instance.UI.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);
            Plugin.Instance.UI.SendMultiKillSound(kPlayer.GamePlayer, kPlayer.MultipleKills);
            kPlayer.CheckKills();
            kPlayer.GamePlayer.OnKilled(cPlayer.GamePlayer);

            if (equipmentUsed != 0)
                OnKill(kPlayer.GamePlayer, cPlayer.GamePlayer, equipmentUsed, kPlayer.Team.Info.KillFeedHexCode,
                    cPlayer.Team.Info.KillFeedHexCode);

            Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Kill, questConditions);
            Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.MultiKill, questConditions);
            Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Killstreak, questConditions);
            if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Headshots, questConditions);

            Plugin.Instance.Quest.CheckQuest(cPlayer.GamePlayer, EQuestType.Death, questConditions);

            _ = Task.Run(async () =>
            {
                await Plugin.Instance.DB.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, xpGained);
                if (cause == EDeathCause.GUN && limb == ELimb.SKULL)
                    await Plugin.Instance.DB.IncreasePlayerHeadshotKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                else
                    await Plugin.Instance.DB.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);

                if (usedKillstreak)
                    await Plugin.Instance.DB.IncreasePlayerKillstreakKillsAsync(kPlayer.GamePlayer.SteamID,
                        killstreakID, 1);
                else if ((kPlayer.GamePlayer.ActiveLoadout.Primary != null &&
                          kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunID == equipmentUsed) ||
                         (kPlayer.GamePlayer.ActiveLoadout.Secondary != null &&
                          kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunID == equipmentUsed))
                {
                    await Plugin.Instance.DB.IncreasePlayerGunXPAsync(kPlayer.GamePlayer.SteamID, equipmentUsed,
                        xpGained);
                    await Plugin.Instance.DB.IncreasePlayerGunKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                }
                else if (kPlayer.GamePlayer.ActiveLoadout.Lethal != null &&
                         kPlayer.GamePlayer.ActiveLoadout.Lethal.Gadget.GadgetID == equipmentUsed)
                    await Plugin.Instance.DB.IncreasePlayerGadgetKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed,
                        1);
                else if (kPlayer.GamePlayer.ActiveLoadout.Knife != null &&
                         kPlayer.GamePlayer.ActiveLoadout.Knife.Knife.KnifeID == equipmentUsed)
                    await Plugin.Instance.DB.IncreasePlayerKnifeKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed,
                        1);
            });
        });
    }

    public override void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow)
    {
        var player = GetCTFPlayer(parameters.player);
        if (player == null) return;

        parameters.applyGlobalArmorMultiplier = IsHardcore;
        if (GamePhase != EGamePhase.Started)
        {
            shouldAllow = false;
            return;
        }

        if (player.GamePlayer.HasSpawnProtection)
        {
            Logging.Debug(
                $"{player.GamePlayer.Player.CharacterName} got damaged, but damaged got ignored due to the player having spawn prot. {player.GamePlayer.SpawnProtectionRemover == null}");
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

        parameters.damage -=
            (player.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageReducePerkName,
                out var damageReducerPerk)
                ? (float)damageReducerPerk.Perk.SkillLevel / 100
                : 0f) * parameters.damage;

        player.GamePlayer.OnDamaged(parameters.killer);

        var kPlayer = GetCTFPlayer(parameters.killer);
        if (kPlayer == null) return;

        if (kPlayer.Team == player.Team && kPlayer != player)
        {
            shouldAllow = false;
            return;
        }

        parameters.damage +=
            (kPlayer.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageIncreasePerkName,
                out var damageIncreaserPerk)
                ? (float)damageIncreaserPerk.Perk.SkillLevel / 100
                : 0f) * parameters.damage;

        if (parameters.cause == EDeathCause.GRENADE && parameters.damage < player.GamePlayer.Player.Player.life.health)
            Plugin.Instance.UI.ShowXPUI(kPlayer.GamePlayer, Config.Medals.FileData.LethalHitXP,
                Plugin.Instance.Translate("Lethal_Hit"));

        if (kPlayer.GamePlayer.HasSpawnProtection)
        {
            kPlayer.GamePlayer.SpawnProtectionRemover.Stop();
            kPlayer.GamePlayer.HasSpawnProtection = false;
        }
    }

    public override void OnPlayerRevived(UnturnedPlayer player)
    {
        var cPlayer = GetCTFPlayer(player);
        if (cPlayer == null) return;

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
        if (cPlayer == null) return;

        if (!SpawnPoints.TryGetValue(cPlayer.Team.SpawnPoint, out var spawnPoints)) return;

        if (spawnPoints.Count == 0) return;

        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        respawnPosition = spawnPoint.GetSpawnPoint();
        yaw = spawnPoint.Yaw;
        player.GiveSpawnProtection(Config.CTF.FileData.SpawnProtectionSeconds);
    }

    public override void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow)
    {
        var cPlayer = GetCTFPlayer(player.Player);
        if (cPlayer == null) return;

        Logging.Debug($"{player.Player.CharacterName} is trying to pick up item {itemData.item.id}");
        var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;

        if (player.Player.Player.equipment.isBusy)
        {
            shouldAllow = false;
            return;
        }

        Dictionary<EQuestCondition, int> questConditions = new()
        {
            { EQuestCondition.Map, Location.LocationID },
            { EQuestCondition.Gamemode, (int)GameMode }
        };

        if (cPlayer.Team.FlagID == itemData.item.id)
        {
            Logging.Debug(
                $"{player.Player.CharacterName} is trying to pick up their own flag, checking if they are saving the flag");
            shouldAllow = false;

            if (!cPlayer.Team.HasFlag)
            {
                Logging.Debug(
                    $"{player.Player.CharacterName} is saving their flag, clearing the flag and putting it back into position");
                Logging.Debug(
                    $"Spawning their team's flag at {cPlayer.Team.FlagSP} for location {Location.LocationName}");
                ItemManager.ServerClearItemsInSphere(itemData.point, 1);
                ItemManager.dropItem(new(cPlayer.Team.FlagID, true), cPlayer.Team.FlagSP, true, true, true);
                cPlayer.Team.HasFlag = true;
                cPlayer.Score += Config.Points.FileData.FlagSavedPoints;
                cPlayer.XP += Config.Medals.FileData.FlagSavedXP;
                cPlayer.FlagsSaved++;
                Plugin.Instance.UI.ShowXPUI(cPlayer.GamePlayer, Config.Medals.FileData.FlagSavedXP,
                    Plugin.Instance.Translate("Flag_Saved").ToRich());
                Plugin.Instance.UI.SendFlagSavedSound(cPlayer.GamePlayer);

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    Plugin.Instance.UI.UpdateCTFHUD(Players, cPlayer.Team);
                    Plugin.Instance.UI.SendCTFFlagStates(cPlayer.Team, (ETeam)cPlayer.Team.TeamID, Players,
                        EFlagState.Recovered);
                    Plugin.Instance.Quest.CheckQuest(player, EQuestType.FlagsSaved, questConditions);
                });

                _ = Task.Run(async () =>
                {
                    await Plugin.Instance.DB.IncreasePlayerFlagsSavedAsync(cPlayer.GamePlayer.SteamID, 1);
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(cPlayer.GamePlayer.SteamID,
                        Config.Medals.FileData.FlagSavedXP);
                });
                return;
            }

            if (!cPlayer.IsCarryingFlag)
            {
                Logging.Debug($"{player.Player.CharacterName} is not carrying an enemy's flag");
                return;
            }

            Logging.Debug(
                $"{player.Player.CharacterName} is carrying the enemy's flag, getting the flag, other team lost flag {otherTeam.HasFlag}");
            if (player.Player.Player.clothing.backpack == otherTeam.FlagID && !otherTeam.HasFlag)
            {
                player.Player.Player.clothing.thirdClothes.backpack = 0;
                player.Player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);

                ItemManager.dropItem(new(otherTeam.FlagID, true), otherTeam.FlagSP, true, true, true);
                Logging.Debug(
                    $"Spawning the other team's flag at {otherTeam.FlagSP} for location {Location.LocationName}");
                otherTeam.HasFlag = true;
                cPlayer.Team.Score++;
                cPlayer.Score += Config.Points.FileData.FlagCapturedPoints;
                cPlayer.XP += Config.Medals.FileData.FlagCapturedXP;
                cPlayer.FlagsCaptured++;
                player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false,
                    false);

                Plugin.Instance.UI.ShowXPUI(cPlayer.GamePlayer, Config.Medals.FileData.FlagCapturedXP,
                    Plugin.Instance.Translate("Flag_Captured").ToRich());
                Plugin.Instance.UI.SendFlagCapturedSound(cPlayer.GamePlayer);

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    Plugin.Instance.UI.UpdateCTFHUD(Players, cPlayer.Team);
                    Plugin.Instance.UI.UpdateCTFHUD(Players, otherTeam);
                    Plugin.Instance.UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players,
                        EFlagState.Captured);
                    Plugin.Instance.Quest.CheckQuest(player, EQuestType.FlagsCaptured, questConditions);
                });

                if (cPlayer.Team.Score >= Config.CTF.FileData.ScoreLimit)
                    _ = Plugin.Instance.StartCoroutine(GameEnd(cPlayer.Team));

                _ = Task.Run(async () =>
                {
                    await Plugin.Instance.DB.IncreasePlayerFlagsCapturedAsync(cPlayer.GamePlayer.SteamID, 1);
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(cPlayer.GamePlayer.SteamID,
                        Config.Medals.FileData.FlagCapturedXP);
                });
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
                TaskDispatcher.QueueOnMainThread(() =>
                    Plugin.Instance.UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players,
                        EFlagState.Taken));
                otherTeam.HasFlag = false;
            }
            else
                TaskDispatcher.QueueOnMainThread(() =>
                    Plugin.Instance.UI.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players,
                        EFlagState.Picked));

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

            player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, true, false);
            cPlayer.IsCarryingFlag = true;
            Plugin.Instance.UI.UpdateCTFHUD(Players, otherTeam);
        }
    }

    public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
    {
        var tPlayer = GetCTFPlayer(player.Player);
        if (tPlayer == null) return;

        if (text.Substring(0, 1) == "/") return;

        isVisible = false;
        TaskDispatcher.QueueOnMainThread(() =>
        {
            var data = player.Data;
            if (data.IsMuted)
            {
                var expiryTime = data.MuteExpiry.UtcDateTime - DateTime.UtcNow;
                Utility.Say(player.Player,
                    $"<color=red>You are muted for{(expiryTime.Days == 0 ? "" : $" {expiryTime.Days} Days ")}{(expiryTime.Hours == 0 ? "" : $" {expiryTime.Hours} Hours")} {expiryTime.Minutes} Minutes");
                return;
            }

            var iconLink = Plugin.Instance.DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "";
            var updatedText =
                $"[{Utility.ToFriendlyName(chatMode)}] <color={Utility.GetLevelColor(player.Data.Level)}>[{player.Data.Level}]</color> <color={tPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={tPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

            var loopPlayers = chatMode == EChatMode.GLOBAL ? Players : Players.Where(k => k.Team == tPlayer.Team);
            foreach (var reciever in loopPlayers)
                //ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: iconLink, useRichTextFormatting: true);
                ChatManager.serverSendMessage(updatedText, Color.white,
                    toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: player.Data.AvatarLink,
                    useRichTextFormatting: true);
        });
    }

    public override void OnVoiceChatUpdated(GamePlayer player)
    {
        if (GamePhase == EGamePhase.Ending)
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
        if (!SpawnPoints.TryGetValue(player.Team.SpawnPoint, out var spawnPoints)) return;

        if (spawnPoints.Count == 0) return;

        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), spawnPoint.Yaw);
        player.GamePlayer.GiveSpawnProtection(Config.CTF.FileData.SpawnProtectionSeconds);
    }

    public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
    {
        var cPlayer = GetCTFPlayer(player.Player);
        if (cPlayer == null) return;

        if (throwable.equippedThrowableAsset.id == (player.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0))
            player.UsedLethal();
        else if (throwable.equippedThrowableAsset.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0))
            player.UsedTactical();
    }

    public override void PlayerConsumeableUsed(GamePlayer player, ItemConsumeableAsset consumeableAsset)
    {
        if (IsPlayerIngame(player.SteamID) &&
            consumeableAsset.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0)) player.UsedTactical();
    }

    public override void PlayerBarricadeSpawned(GamePlayer player, BarricadeDrop drop)
    {
        if (!IsPlayerIngame(player.SteamID)) return;

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

        var turret = player.ActiveLoadout.Killstreaks.FirstOrDefault(k =>
            k.Killstreak.KillstreakInfo.IsTurret && drop.asset.id == k.Killstreak.KillstreakInfo.TurretID);
        if (turret == null || drop.interactable is not InteractableSentry sentry) return;

        if (sentry.items.tryAddItem(new(turret.Killstreak.KillstreakInfo.GunID, true), true))
        {
            sentry.despawnWhenDestroyed = true;
            sentry.refreshDisplay();
        }

        GameTurrets.Add(player, (drop, turret.Killstreak.KillstreakInfo));
        GameTurretsInverse.Add(drop, player);
        GameTurretDamager.Add(drop,
            Plugin.Instance.StartCoroutine(DamageTurret(drop, turret.Killstreak.KillstreakInfo.TurretDamagePerSecond)));
    }

    public override void PlayerBarricadeDamaged(
        GamePlayer player,
        BarricadeDrop drop,
        ref ushort pendingTotalDamage,
        ref bool shouldAllow)
    {
        var damager = GetCTFPlayer(player.Player);
        if (damager == null) return;

        if (!GameTurretsInverse.TryGetValue(drop, out var gPlayer)) return;

        var owner = GetCTFPlayer(gPlayer.Player);
        if (owner == null) return;

        if (owner.Team == damager.Team)
        {
            shouldAllow = false;
            return;
        }

        var barricadeData = drop.GetServersideData();
        if (barricadeData == null) return;

        if (barricadeData.barricade.health > pendingTotalDamage) return;

        Plugin.Instance.UI.ShowXPUI(player, Config.Medals.FileData.TurretDestroyXP,
            Plugin.Instance.Translate("Turret_Destroy"));
        _ = Task.Run(async () =>
            await Plugin.Instance.DB.IncreasePlayerXPAsync(player.SteamID, Config.Medals.FileData.TurretDestroyXP));
    }

    public override void PlayerChangeFiremode(GamePlayer player)
    {
        var cPlayer = GetCTFPlayer(player.Player);
        if (cPlayer == null) return;

        if (GamePhase == EGamePhase.Ending) return;

        if (player.ScoreboardCooldown > DateTime.UtcNow) return;

        player.ScoreboardCooldown = DateTime.UtcNow.AddSeconds(0.5);

        if (cPlayer.GamePlayer.HasScoreboard)
        {
            cPlayer.GamePlayer.HasScoreboard = false;
            Plugin.Instance.UI.HideCTFLeaderboard(cPlayer.GamePlayer);
        }
        else
        {
            cPlayer.GamePlayer.HasScoreboard = true;
            var wonTeam = BlueTeam.Score > RedTeam.Score ? BlueTeam :
                RedTeam.Score > BlueTeam.Score ? RedTeam : new(-1, true, new(), 0, Vector3.zero);
            Plugin.Instance.UI.SetupCTFLeaderboard(cPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true,
                IsHardcore);
            Plugin.Instance.UI.ShowCTFLeaderboard(cPlayer.GamePlayer);
        }
    }

    public override void PlayerStanceChanged(PlayerStance obj)
    {
        var cPlayer = GetCTFPlayer(obj.player);
        if (cPlayer == null) return;

        cPlayer.GamePlayer.OnStanceChanged(obj.stance);
    }

    public override void PlayerEquipmentChanged(GamePlayer player)
    {
        var cPlayer = GetCTFPlayer(player.Player);
        if (cPlayer == null) return;

        if (GamePhase == EGamePhase.Starting) return;

        player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming,
            cPlayer.IsCarryingFlag, true);
    }

    public override void PlayerAimingChanged(GamePlayer player, bool isAiming)
    {
        var cPlayer = GetCTFPlayer(player.Player);
        if (cPlayer == null) return;

        if (GamePhase == EGamePhase.Starting) return;

        player.GiveMovement(isAiming, cPlayer.IsCarryingFlag, false);
    }

    public CTFPlayer GetCTFPlayer(CSteamID steamID) =>
        PlayersLookup.TryGetValue(steamID, out var cPlayer) ? cPlayer : null;

    public CTFPlayer GetCTFPlayer(UnturnedPlayer player) =>
        PlayersLookup.TryGetValue(player.CSteamID, out var cPlayer) ? cPlayer : null;

    public CTFPlayer GetCTFPlayer(Player player) =>
        PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out var cPlayer) ? cPlayer : null;

    public override bool IsPlayerIngame(CSteamID steamID) => PlayersLookup.ContainsKey(steamID);

    public override int GetPlayerCount() => Players.Count;

    public override void PlayerPickupItem(
        UnturnedPlayer player,
        InventoryGroup inventoryGroup,
        byte inventoryIndex,
        ItemJar P)
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