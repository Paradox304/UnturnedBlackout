using Rocket.Core.Utils;
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
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.GameTypes;

public class FFAGame : Game
{
    public List<FFASpawnPoint> SpawnPoints { get; set; }
    public List<FFASpawnPoint> UnavailableSpawnPoints { get; set; }

    public List<FFAPlayer> Players { get; set; }
    public Dictionary<CSteamID, FFAPlayer> PlayersLookup { get; set; }

    public Coroutine GameStarter { get; set; }
    public Coroutine GameEnder { get; set; }

    public uint Frequency { get; set; }

    public FFAGame(ArenaLocation location, bool isHardcore) : base(EGameType.FFA, location, isHardcore)
    {
        SpawnPoints = Plugin.Instance.Data.Data.FFASpawnPoints.Where(k => k.LocationID == location.LocationID).ToList();
        Players = new();
        PlayersLookup = new();
        UnavailableSpawnPoints = new();
        Frequency = Utility.GetFreeFrequency();
    }

    public override void ForceStartGame()
    {
        GameStarter = Plugin.Instance.StartCoroutine(StartGame());
    }

    public override void ForceEndGame()
    {
        _ = Plugin.Instance.StartCoroutine(GameEnd());
    }


    public IEnumerator StartGame()
    {
        TaskDispatcher.QueueOnMainThread(CleanMap);
        GamePhase = EGamePhase.STARTING;
        UI.OnGameUpdated();
        foreach (var player in Players)
        {
            if (player.GamePlayer.IsLoading)
                continue;

            UI.ClearWaitingForPlayersUI(player.GamePlayer);
            player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
            UI.ShowCountdownUI(player.GamePlayer);
            SpawnPlayer(player);
        }

        for (var seconds = Config.FFA.FileData.StartSeconds; seconds >= 0; seconds--)
        {
            yield return new WaitForSeconds(1);

            foreach (var player in Players)
                UI.SendCountdownSeconds(player.GamePlayer, seconds);
        }

        GamePhase = EGamePhase.STARTED;
        UI.OnGameUpdated();
        foreach (var player in Players)
        {
            player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);

            UI.SendFFAHUD(player.GamePlayer);
            UI.ClearCountdownUI(player.GamePlayer);
            UI.UpdateFFATopUI(player, Players);
            player.StartTime = DateTime.UtcNow;
        }

        GameEnder = Plugin.Instance.StartCoroutine(EndGame());
    }

    public IEnumerator EndGame()
    {
        for (var seconds = Config.FFA.FileData.EndSeconds; seconds >= 0; seconds--)
        {
            yield return new WaitForSeconds(1);

            var timeSpan = TimeSpan.FromSeconds(seconds);
            foreach (var player in Players)
                UI.UpdateFFATimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
        }

        _ = Plugin.Instance.StartCoroutine(GameEnd());
    }

    public IEnumerator GameEnd()
    {
        GameEnder.Stop();
        GamePhase = EGamePhase.ENDING;
        UI.OnGameUpdated();
        Dictionary<GamePlayer, MatchEndSummary> summaries = new();

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

            Plugin.Instance.Discord.SendEmbed(new(null, null, null, Utility.GetDiscordColorCode(@case.CaseRarity), DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon), new(roundEndCasePlayer.Data.SteamName, $"https://steamcommunity.com/profiles/{roundEndCasePlayer.SteamID}", roundEndCasePlayer.Data.AvatarLinks[0]), new Field[] { new($"[{@case.CaseRarity.ToFriendlyName()}] {@case.CaseName}", "‎", true) }, new(@case.IconLink), null), null, Plugin.Instance.Config.Webhooks.FileData.CaseDroppedWebhookLink);

            roundEndCases.Add((roundEndCasePlayer, @case));
            DB.IncreasePlayerCase(roundEndCasePlayer.SteamID, @case.CaseID, 1);
        }

        for (var index = 0; index < Players.Count; index++)
        {
            var player = Players[index];
            UI.ClearFFAHUD(player.GamePlayer);
            UI.ClearMidgameLoadoutUI(player.GamePlayer);
            if (player.GamePlayer.Player.Player.life.isDead)
                player.GamePlayer.Player.Player.life.ServerRespawn(false);

            UI.RemoveKillCard(player.GamePlayer);
            UI.ClearAnimations(player.GamePlayer);

            if (player.GamePlayer.HasScoreboard)
            {
                player.GamePlayer.HasScoreboard = false;
                UI.HideFFALeaderboard(player.GamePlayer);
            }

            UI.SetupPreEndingUI(player.GamePlayer, EGameType.FFA, index == 0, 0, 0, "", "");
            MatchEndSummary summary = new(player.GamePlayer, player.XP, player.StartingLevel, player.StartingXP, player.Kills, player.Deaths, player.Assists, player.HighestKillstreak, player.HighestMK, player.StartTime, GameMode, index == 0);
            summaries.Add(player.GamePlayer, summary);

            DB.IncreasePlayerXP(player.GamePlayer.SteamID, summary.PendingXP);
            DB.IncreasePlayerCredits(player.GamePlayer.SteamID, summary.PendingCredits);
            DB.IncreasePlayerBPXP(player.GamePlayer.SteamID, summary.BattlepassXP + summary.BattlepassBonusXP);

            TaskDispatcher.QueueOnMainThread(() => Quest.CheckQuest(player.GamePlayer, EQuestType.FINISH_MATCH, new() { { EQuestCondition.MAP, Location.LocationID }, { EQuestCondition.GAMEMODE, (int)GameMode }, { EQuestCondition.WIN_KILLS, player.Kills } }));
            if (index == 0)
                TaskDispatcher.QueueOnMainThread(() => Quest.CheckQuest(player.GamePlayer, EQuestType.WIN, new() { { EQuestCondition.MAP, Location.LocationID }, { EQuestCondition.GAMEMODE, (int)GameMode }, { EQuestCondition.WIN_KILLS, player.Kills } }));
        }

        TaskDispatcher.QueueOnMainThread(() =>
        {
            UI.SetupFFALeaderboard(Players, Location, false, IsHardcore);
            CleanMap();
        });

        yield return new WaitForSeconds(5);

        foreach (var player in Players)
            UI.ShowFFALeaderboard(player.GamePlayer);

        if (roundEndCases.Count > 0)
            _ = Plugin.Instance.StartCoroutine(UI.SetupRoundEndDrops(Players.Select(k => k.GamePlayer).ToList(), roundEndCases, 1));

        yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds);

        foreach (var player in Players.ToList())
        {
            RemovePlayerFromGame(player.GamePlayer);
            Plugin.Instance.Game.SendPlayerToLobby(player.GamePlayer.Player, summaries.TryGetValue(player.GamePlayer, out var pendingSummary) ? pendingSummary : null);
        }

        Players = new();

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

        FFAPlayer fPlayer = new(player);

        player.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
        Players.Add(fPlayer);
        if (PlayersLookup.ContainsKey(player.SteamID))
            _ = PlayersLookup.Remove(player.SteamID);

        PlayersLookup.Add(player.SteamID, fPlayer);
        UI.OnGameCountUpdated(this);

        UI.SendLoadingUI(player.Player, true, GameMode, Location);
        for (var seconds = 1; seconds <= 5; seconds++)
        {
            yield return new WaitForSeconds(1);

            UI.UpdateLoadingBar(player.Player, new('　', Math.Min(96, seconds * 96 / 5)));
        }

        var currentPos = player.Player.Position;
        player.Player.Player.teleportToLocationUnsafe(new(currentPos.x, currentPos.y + 100, currentPos.z), 0);
        GiveLoadout(fPlayer);
        UI.SendPreEndingUI(fPlayer.GamePlayer);

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
                    foreach (var ply in Players.Where(ply => ply != fPlayer))
                        UI.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
                }

                SpawnPlayer(fPlayer);
                break;
            case EGamePhase.STARTING:
                player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                UI.ShowCountdownUI(player);
                SpawnPlayer(fPlayer);
                break;
            case EGamePhase.ENDING:
                UI.SetupFFALeaderboard(fPlayer, Players, Location, true, IsHardcore);
                UI.ShowFFALeaderboard(fPlayer.GamePlayer);
                break;
            default:
                UI.SendFFAHUD(player);
                UI.UpdateFFATopUI(fPlayer, Players);
                SpawnPlayer(fPlayer);
                break;
        }

        UI.ClearLoadingUI(player.Player);
        UI.SendVoiceChatUI(player);
    }

    public override void RemovePlayerFromGame(GamePlayer player)
    {
        var fPlayer = GetFFAPlayer(player.Player);

        if (fPlayer == null)
            return;

        UI.ClearPreEndingUI(player);
        UI.ClearFFAHUD(player);
        UI.ClearVoiceChatUI(player);
        UI.ClearKillstreakUI(player);
        OnStoppedTalking(player);

        switch (GamePhase)
        {
            case EGamePhase.STARTING:
                UI.ClearCountdownUI(player);
                fPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
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

        player.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, 0);
        fPlayer.GamePlayer.OnGameLeft();
        _ = Players.Remove(fPlayer);
        _ = PlayersLookup.Remove(fPlayer.GamePlayer.SteamID);

        foreach (var ply in Players)
            UI.UpdateFFATopUI(ply, Players);

        UI.OnGameCountUpdated(this);
    }

    public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
    {
        var fPlayer = GetFFAPlayer(player);
        if (fPlayer == null)
            return;

        if (cause == EDeathCause.SUICIDE)
        {
            RemovePlayerFromGame(fPlayer.GamePlayer);
            return;
        }

        if (fPlayer.GamePlayer.HasScoreboard)
        {
            fPlayer.GamePlayer.HasScoreboard = false;
            UI.HideFFALeaderboard(fPlayer.GamePlayer);
        }

        var victimKS = fPlayer.Killstreak;
        var updatedKiller = cause == EDeathCause.WATER ? fPlayer.GamePlayer.SteamID : cause is EDeathCause.LANDMINE or EDeathCause.SHRED ? fPlayer.GamePlayer.LastDamager.Count > 0 ? fPlayer.GamePlayer.LastDamager.Pop() : killer : killer;

        Logging.Debug($"Game player died, player name: {fPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");
        fPlayer.OnDeath(updatedKiller);
        fPlayer.GamePlayer.OnDeath(updatedKiller, Config.FFA.FileData.RespawnSeconds);
        DB.IncreasePlayerDeaths(fPlayer.GamePlayer.SteamID, 1);

        TaskDispatcher.QueueOnMainThread(() =>
        {
            var kPlayer = GetFFAPlayer(updatedKiller);
            if (kPlayer == null)
            {
                Logging.Debug("Killer not found, returning");
                return;
            }

            if (kPlayer.GamePlayer.SteamID == fPlayer.GamePlayer.SteamID)
            {
                OnKill(kPlayer.GamePlayer, fPlayer.GamePlayer, 0, Config.FFA.FileData.FFATeam.KillFeedHexCode, Config.FFA.FileData.FFATeam.KillFeedHexCode, false, cause == EDeathCause.WATER ? SUICIDE_SYMBOL : EXPLOSION_SYMBOL);

                Logging.Debug("Player killed themselves, returning");
                return;
            }

            Dictionary<EQuestCondition, int> questConditions = new() { { EQuestCondition.MAP, Location.LocationID }, { EQuestCondition.GAMEMODE, (int)GameMode } };

            Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");

            if (fPlayer.GamePlayer.LastDamager.Count > 0 && fPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                _ = fPlayer.GamePlayer.LastDamager.Pop();

            if (fPlayer.GamePlayer.LastDamager.Count > 0)
            {
                var assister = GetFFAPlayer(fPlayer.GamePlayer.LastDamager.Pop());
                if (assister != null && assister != kPlayer)
                {
                    assister.Assists++;
                    assister.Score += Config.Points.FileData.AssistPoints;
                    if (!assister.GamePlayer.Player.Player.life.isDead)
                        UI.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP, Plugin.Instance.Translate("Assist_Kill", fPlayer.GamePlayer.Player.CharacterName.ToUnrich()));

                    DB.IncreasePlayerXP(assister.GamePlayer.SteamID, Config.Medals.FileData.AssistKillXP);
                }

                fPlayer.GamePlayer.LastDamager.Clear();
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
                overrideSymbol = MELEE_SYMBOL;
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

            if (kPlayer.PlayersKilled.ContainsKey(fPlayer.GamePlayer.SteamID))
            {
                kPlayer.PlayersKilled[fPlayer.GamePlayer.SteamID] += 1;
                if (kPlayer.PlayersKilled[fPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                {
                    xpGained += Config.Medals.FileData.DominationXP;
                    xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                    Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.DOMINATION, questConditions);
                }
            }
            else
                kPlayer.PlayersKilled.Add(fPlayer.GamePlayer.SteamID, 1);

            if (fPlayer.GamePlayer.SteamID == kPlayer.GamePlayer.LastKiller)
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

            if (!usedKillstreak && cause == EDeathCause.GUN && (fPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
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
            kPlayer.GamePlayer.OnKilled(fPlayer.GamePlayer);

            if (equipmentUsed != 0)
                OnKill(kPlayer.GamePlayer, fPlayer.GamePlayer, equipmentUsed, Config.FFA.FileData.FFATeam.KillFeedHexCode, Config.FFA.FileData.FFATeam.KillFeedHexCode, cause == EDeathCause.GUN && limb == ELimb.SKULL, overrideSymbol);

            foreach (var ply in Players)
                UI.UpdateFFATopUI(ply, Players);

            if (kPlayer.Kills == Config.FFA.FileData.ScoreLimit)
                _ = Plugin.Instance.StartCoroutine(GameEnd());

            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.KILL, questConditions);
            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.MULTI_KILL, questConditions);
            Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.KILLSTREAK, questConditions);
            if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.HEADSHOTS, questConditions);

            Quest.CheckQuest(fPlayer.GamePlayer, EQuestType.DEATH, questConditions);

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
        var player = GetFFAPlayer(parameters.player);
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

        var damageReducePercent = player.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageReducePerkName, out var damageReducerPerk) ? (float)damageReducerPerk.Perk.SkillLevel / 100 : 0f;
        parameters.damage -= damageReducePercent * parameters.damage;

        player.GamePlayer.OnDamaged(parameters.killer);

        var kPlayer = GetFFAPlayer(parameters.killer);
        if (kPlayer == null)
            return;

        var damageIncreasePercent = kPlayer.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageIncreasePerkName, out var damageIncreaserPerk) ? (float)damageIncreaserPerk.Perk.SkillLevel / 100 : 0f;
        parameters.damage += damageIncreasePercent * parameters.damage;

        Logging.Debug($"cause: {parameters.cause}, damage: {parameters.damage}, player health: {player.GamePlayer.Player.Player.life.health}");
        if (parameters.cause == EDeathCause.GRENADE && parameters.damage < player.GamePlayer.Player.Player.life.health)
        {
            Logging.Debug($"Condition fulfilled, send hit xp for {Config.Medals.FileData.LethalHitXP}");
            UI.ShowXPUI(kPlayer.GamePlayer, Config.Medals.FileData.LethalHitXP, Plugin.Instance.Translate("Lethal_Hit"));
        }

        if (kPlayer.GamePlayer.HasSpawnProtection)
        {
            kPlayer.GamePlayer.SpawnProtectionRemover.Stop();
            kPlayer.GamePlayer.HasSpawnProtection = false;
        }
    }

    public override void OnPlayerRevived(UnturnedPlayer player)
    {
        var fPlayer = GetFFAPlayer(player);
        if (fPlayer == null)
            return;

        fPlayer.GamePlayer.OnRevived();
    }

    public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition, ref float yaw)
    {
        if (!IsPlayerIngame(player.SteamID))
            return;

        var spawnPoint = GetFreeSpawn();
        respawnPosition = spawnPoint.GetSpawnPoint();
        yaw = spawnPoint.Yaw;
        player.GiveSpawnProtection(Config.FFA.FileData.SpawnProtectionSeconds);
    }

    public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
    {
        var fPlayer = GetFFAPlayer(player.Player);
        if (fPlayer == null)
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

            var updatedText =
                $"[{chatMode.ToFriendlyName()}] <color={Utility.GetLevelColor(player.Data.Level)}>[{player.Data.Level}]</color> <color={Config.FFA.FileData.FFATeam.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={Config.FFA.FileData.FFATeam.ChatMessageHexCode}>{text.ToUnrich()}</color>";

            foreach (var reciever in Players)
                ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: player.Data.AvatarLinks[0], useRichTextFormatting: true);
        });
    }

    public override void OnVoiceChatUpdated(GamePlayer player) => SendVoiceChat(Players.Select(k => k.GamePlayer).ToList(), false);

    public void GiveLoadout(FFAPlayer player)
    {
        player.GamePlayer.Player.Player.inventory.ClearInventory();
        Plugin.Instance.Loadout.GiveLoadout(player.GamePlayer);
    }

    public void SpawnPlayer(FFAPlayer player)
    {
        if (SpawnPoints.Count == 0)
            return;

        var spawnPoint = GetFreeSpawn();
        player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), spawnPoint.Yaw);
        player.GamePlayer.GiveSpawnProtection(Config.FFA.FileData.SpawnProtectionSeconds);
    }

    public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
    {
        var fPlayer = GetFFAPlayer(player.Player);
        if (fPlayer == null)
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

        Logging.Debug("Turret spawned");
        if (sentry.items.tryAddItem(new(turret.Killstreak.KillstreakInfo.GunID, true), true))
        {
            Logging.Debug("Successfully added gun in turret");
            sentry.despawnWhenDestroyed = true;
            sentry.refreshDisplay();
        }

        Logging.Debug("Add turret to tracking system");

        GameTurrets.Add(player, (drop, turret.Killstreak.KillstreakInfo));
        GameTurretsInverse.Add(drop, player);
        GameTurretDamager.Add(drop, Plugin.Instance.StartCoroutine(DamageTurret(drop, turret.Killstreak.KillstreakInfo.TurretDamagePerSecond)));
    }

    public override void PlayerBarricadeDamaged(GamePlayer player, BarricadeDrop drop, ref ushort pendingTotalDamage, ref bool shouldAllow)
    {
        var damager = GetFFAPlayer(player.Player);
        if (damager == null)
            return;

        if (!GameTurretsInverse.TryGetValue(drop, out var gPlayer))
            return;

        if (player == gPlayer)
        {
            shouldAllow = false;
            return;
        }

        var owner = GetFFAPlayer(gPlayer.Player);
        if (owner == null)
            return;

        var barricadeData = drop.GetServersideData();
        if (barricadeData == null)
            return;

        Logging.Debug($"Turret damaged by {damager.GamePlayer.Player.CharacterName}, damage: {pendingTotalDamage}, health: {barricadeData.barricade.health}");
        if (barricadeData.barricade.health > pendingTotalDamage)
            return;

        Logging.Debug($"Turret destroyed, send xp");
        UI.ShowXPUI(player, Config.Medals.FileData.TurretDestroyXP, Plugin.Instance.Translate("Turret_Destroy"));
        DB.IncreasePlayerXP(player.SteamID, Config.Medals.FileData.TurretDestroyXP);
    }
    
    public override void PlayerSendScoreboard(GamePlayer gPlayer, bool state)
    {
        var player = GetFFAPlayer(gPlayer.Player);
        if (player == null)
            return;
        
        if (state && !gPlayer.HasScoreboard)
        {
            if (gPlayer.ScoreboardCooldown > DateTime.UtcNow)
                return;
            
            gPlayer.HasScoreboard = true;
            UI.SetupFFALeaderboard(player, Players, Location, true, IsHardcore);
            UI.ShowFFALeaderboard(gPlayer);
        }
        else if (!state && gPlayer.HasScoreboard)
        {
            gPlayer.HasScoreboard = false;
            UI.HideFFALeaderboard(gPlayer);
            gPlayer.ScoreboardCooldown = DateTime.UtcNow.AddSeconds(1);
        }
    }

    public override void PlayerStanceChanged(PlayerStance obj)
    {
        var fPlayer = GetFFAPlayer(obj.player);
        if (fPlayer == null)
            return;

        fPlayer.GamePlayer.OnStanceChanged(obj.stance);
    }

    /*public override void PlayerEquipmentChanged(GamePlayer player)
    {
        if (IsPlayerIngame(player.SteamID) && GamePhase != EGamePhase.STARTING)
            player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, true);
    }

    public override void PlayerAimingChanged(GamePlayer player, bool isAiming)
    {
        if (IsPlayerIngame(player.SteamID) && GamePhase != EGamePhase.STARTING)
            player.GiveMovement(isAiming, false, false);
    }
    */
    
    public IEnumerator SpawnUsedUp(FFASpawnPoint spawnPoint)
    {
        _ = SpawnPoints.Remove(spawnPoint);
        UnavailableSpawnPoints.Add(spawnPoint);
        yield return new WaitForSeconds(Config.Base.FileData.SpawnUnavailableSeconds);

        SpawnPoints.Add(spawnPoint);
        _ = UnavailableSpawnPoints.Remove(spawnPoint);
    }

    public FFAPlayer GetFFAPlayer(CSteamID steamID) => PlayersLookup.TryGetValue(steamID, out var fPlayer) ? fPlayer : null;

    public FFAPlayer GetFFAPlayer(UnturnedPlayer player) => PlayersLookup.TryGetValue(player.CSteamID, out var fPlayer) ? fPlayer : null;

    public FFAPlayer GetFFAPlayer(Player player) => PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out var fPlayer) ? fPlayer : null;

    public FFASpawnPoint GetFreeSpawn() => SpawnPoints.Where(k => !IsPlayerNearPosition(k.GetSpawnPoint(), Location.PositionCheck)).ToList().RandomOrDefault() ?? SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)];

    public bool IsPlayerNearPosition(Vector3 position, float radius) => Players.Exists(k => (k.GamePlayer.Player.Position - position).sqrMagnitude < radius);

    public override bool IsPlayerIngame(CSteamID steamID) => PlayersLookup.ContainsKey(steamID);

    public override int GetPlayerCount() => Players.Count;

    // ReSharper disable once InconsistentNaming
    public override void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
    {
    }

    public override void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow)
    {
    }

    public override List<GamePlayer> GetPlayers() => Players.Select(k => k.GamePlayer).ToList();

    public override bool IsPlayerCarryingFlag(GamePlayer player) => false;

    public override TeamInfo GetTeam(GamePlayer player) => Config.FFA.FileData.FFATeam;
}