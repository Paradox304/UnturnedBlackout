using Rocket.Core.Utils;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.KC;
using UnturnedBlackout.Models.TDM;
using Timer = System.Timers.Timer;

namespace UnturnedBlackout.GameTypes
{
    public class KCGame : Game
    {
        public Dictionary<int, List<TDMSpawnPoint>> SpawnPoints { get; set; }

        public List<KCPlayer> Players { get; set; }
        public Dictionary<CSteamID, KCPlayer> PlayersLookup { get; set; }

        public KCTeam BlueTeam { get; set; }
        public KCTeam RedTeam { get; set; }

        public Coroutine GameStarter { get; set; }
        public Coroutine GameEnder { get; set; }
        public Timer m_SpawnSwitcher { get; set; }

        public uint Frequency { get; set; }

        public KCGame(ArenaLocation location, bool isHardcore) : base(EGameType.KC, location, isHardcore)
        {
            SpawnPoints = new Dictionary<int, List<TDMSpawnPoint>>();
            foreach (var spawnPoint in Plugin.Instance.Data.Data.TDMSpawnPoints.Where(k => k.LocationID == location.LocationID))
            {
                if (SpawnPoints.TryGetValue(spawnPoint.GroupID, out List<TDMSpawnPoint> spawnPoints))
                {
                    spawnPoints.Add(spawnPoint);
                }
                else
                {
                    SpawnPoints.Add(spawnPoint.GroupID, new List<TDMSpawnPoint> { spawnPoint });
                }
            }
            Players = new List<KCPlayer>();
            PlayersLookup = new Dictionary<CSteamID, KCPlayer>();

            var blueTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
            var redTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

            BlueTeam = new KCTeam(this, (byte)ETeam.Blue, false, Config.KC.FileData.BlueDogTagID, blueTeamInfo);
            RedTeam = new KCTeam(this, (byte)ETeam.Red, false, Config.KC.FileData.RedDogTagID, redTeamInfo);
            Frequency = Utility.GetFreeFrequency();

            m_SpawnSwitcher = new Timer(Config.Base.FileData.SpawnSwitchSeconds * 1000);
            m_SpawnSwitcher.Elapsed += SpawnSwitch;
        }

        public IEnumerator StartGame()
        {
            TaskDispatcher.QueueOnMainThread(() => WipeItems());
            GamePhase = EGamePhase.Starting;
            foreach (var player in Players)
            {
                Plugin.Instance.UI.ClearWaitingForPlayersUI(player.GamePlayer);
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UI.ShowCountdownUI(player.GamePlayer);
                SpawnPlayer(player);
            }

            for (int seconds = Config.KC.FileData.StartSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                foreach (var player in Players)
                {
                    Plugin.Instance.UI.SendCountdownSeconds(player.GamePlayer, seconds);
                }
            }
            GamePhase = EGamePhase.Started;

            foreach (var player in Players)
            {
                player.GamePlayer.GiveMovement(player.GamePlayer.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, false);
                player.StartTime = DateTime.UtcNow;
                Plugin.Instance.UI.SendKCHUD(player, BlueTeam, RedTeam);
                Plugin.Instance.UI.ClearCountdownUI(player.GamePlayer);
            }

            m_SpawnSwitcher.Start();
            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.KC.FileData.EndSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                foreach (var player in Players)
                {
                    Plugin.Instance.UI.UpdateKCTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
                }
            }

            KCTeam wonTeam;
            if (BlueTeam.Score > RedTeam.Score)
            {
                wonTeam = BlueTeam;
            }
            else if (RedTeam.Score > BlueTeam.Score)
            {
                wonTeam = RedTeam;
            }
            else
            {
                wonTeam = new KCTeam(this, -1, true, 0, new TeamInfo());
            }
            Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
        }

        public IEnumerator GameEnd(KCTeam wonTeam)
        {
            if (GameEnder != null)
            {
                Plugin.Instance.StopCoroutine(GameEnder);
            }

            GamePhase = EGamePhase.Ending;

            Plugin.Instance.UI.OnGameUpdated();

            var endTime = DateTime.UtcNow;
            var roundEndCasesPlayers = new List<GamePlayer>();
            foreach (var player in Players)
            {
                var totalMinutesPlayed = (int)(endTime - player.StartTime).TotalMinutes;
                if (totalMinutesPlayed < Config.RoundEndCases.FileData.MinimumMinutesPlayed || player.Kills == 0)
                {
                    continue;
                }

                var chance = Config.RoundEndCases.FileData.Chance * totalMinutesPlayed;
                if (UnityEngine.Random.Range(1, 101) > chance)
                {
                    continue;
                }
                roundEndCasesPlayers.Add(player.GamePlayer);
                if (roundEndCasesPlayers.Count == 8) break;
            }

            var roundEndCases = new List<(GamePlayer, Case)>();
            foreach (var roundEndCasePlayer in roundEndCasesPlayers)
            {
                var @case = GetRandomRoundEndCase();
                if (@case == null)
                {
                    Logging.Debug($"Random case is null");
                    continue;
                }

                Logging.Debug($"Random case is not null, add the case and give it to player");
                roundEndCases.Add((roundEndCasePlayer, @case));
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DB.IncreasePlayerCaseAsync(roundEndCasePlayer.SteamID, @case.CaseID, 1);
                });
            }

            var summaries = new Dictionary<GamePlayer, MatchEndSummary>();
            foreach (var player in Players)
            {
                Plugin.Instance.UI.ClearKCHUD(player.GamePlayer);
                Plugin.Instance.UI.ClearMidgameLoadoutUI(player.GamePlayer);

                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UI.HideKCLeaderboard(player.GamePlayer);
                }
                var summary = new MatchEndSummary(player.GamePlayer, player.XP, player.StartingLevel, player.StartingXP, player.Kills, player.Deaths, player.Assists, player.HighestKillstreak, player.HighestMK, player.StartTime, GameMode, player.Team == wonTeam);
                summaries.Add(player.GamePlayer, summary);
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(player.GamePlayer.SteamID, summary.PendingXP);
                    await Plugin.Instance.DB.IncreasePlayerCreditsAsync(player.GamePlayer.SteamID, summary.PendingCredits);
                    await Plugin.Instance.DB.IncreasePlayerBPXPAsync(player.GamePlayer.SteamID, summary.BattlepassXP + summary.BattlepassBonusXP);
                });

                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(player.GamePlayer, EQuestType.FinishMatch, new Dictionary<EQuestCondition, int> { { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode }, { EQuestCondition.WinKills, player.Kills }, { EQuestCondition.WinTags, player.KillsDenied + player.KillsConfirmed } }));
                if (player.Team == wonTeam)
                {
                    TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(player.GamePlayer, EQuestType.Win, new Dictionary<EQuestCondition, int> { { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode }, { EQuestCondition.WinKills, player.Kills }, { EQuestCondition.WinTags, player.KillsDenied + player.KillsConfirmed } }));
                }
                Plugin.Instance.UI.SetupPreEndingUI(player.GamePlayer, EGameType.KC, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName);
                player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UI.SetupKCLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false, IsHardcore);
                WipeItems();
            });
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UI.ShowKCLeaderboard(player.GamePlayer);
            }

            if (roundEndCases.Count > 0)
            {
                Plugin.Instance.StartCoroutine(Plugin.Instance.UI.SetupRoundEndDrops(Players.Select(k => k.GamePlayer).ToList(), roundEndCases, 2));
            }

            yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds);
            foreach (var player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.Game.SendPlayerToLobby(player.GamePlayer.Player, summaries.TryGetValue(player.GamePlayer, out MatchEndSummary pendingSummary) ? pendingSummary : null);
            }

            Players = new List<KCPlayer>();
            BlueTeam.Destroy();
            RedTeam.Destroy();
            if (m_SpawnSwitcher.Enabled)
            {
                m_SpawnSwitcher.Stop();
            }

            var locations = Plugin.Instance.Game.AvailableLocations;
            lock (locations)
            {
                var locString = "";
                foreach (var loc in locations)
                {
                    var locc = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == loc);
                    locString += $"{locc.LocationName},";
                }
                Logging.Debug($"Game ending, locations available: {locString}");
                var randomLocation = locations.Count > 0 ? locations[UnityEngine.Random.Range(0, locations.Count)] : Location.LocationID;
                var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == randomLocation);
                var gameMode = Plugin.Instance.Game.GetRandomGameMode(location.LocationID);
                GamePhase = EGamePhase.Ended;
                Plugin.Instance.Game.EndGame(this);
                Plugin.Instance.Game.StartGame(location, gameMode.Item1, gameMode.Item2);
            }
        }

        public override IEnumerator AddPlayerToGame(GamePlayer player)
        {
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                yield break;
            }
            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;
            KCPlayer kPlayer = new(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(kPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, kPlayer);

            Plugin.Instance.UI.OnGameCountUpdated(this);
            Plugin.Instance.UI.SendLoadingUI(player.Player, true, GameMode, Location);
            for (int seconds = 1; seconds <= 5; seconds++)
            {
                yield return new WaitForSeconds(1);
                Plugin.Instance.UI.UpdateLoadingBar(player.Player, new string('　', Math.Min(96, seconds * 96 / 5)));
            }
            var currentPos = player.Player.Position;
            player.Player.Player.teleportToLocationUnsafe(new Vector3(currentPos.x, currentPos.y + 100, currentPos.z), 0);
            GiveLoadout(kPlayer);
            Plugin.Instance.UI.SendPreEndingUI(kPlayer.GamePlayer);
            SpawnPlayer(kPlayer);
            Plugin.Instance.UI.ClearLoadingUI(player.Player);
            Plugin.Instance.UI.SendVoiceChatUI(player);

            switch (GamePhase)
            {
                case EGamePhase.WaitingForPlayers:
                    var minPlayers = Location.GetMinPlayers(GameMode);
                    if (Players.Count >= minPlayers)
                    {
                        GameStarter = Plugin.Instance.StartCoroutine(StartGame());
                    }
                    else
                    {
                        Plugin.Instance.UI.SendWaitingForPlayersUI(player, Players.Count, minPlayers);
                        foreach (var ply in Players)
                        {
                            if (ply == kPlayer)
                            {
                                continue;
                            }

                            Plugin.Instance.UI.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
                        }
                    }
                    break;
                case EGamePhase.Starting:
                    player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                    Plugin.Instance.UI.ShowCountdownUI(player);
                    break;
                case EGamePhase.Ending:
                    KCTeam wonTeam;
                    if (BlueTeam.Score > RedTeam.Score)
                    {
                        wonTeam = BlueTeam;
                    }
                    else if (RedTeam.Score > BlueTeam.Score)
                    {
                        wonTeam = RedTeam;
                    }
                    else
                    {
                        wonTeam = new KCTeam(this, -1, true, 0, new TeamInfo());
                    }
                    Plugin.Instance.UI.SetupKCLeaderboard(kPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
                    Plugin.Instance.UI.ShowKCLeaderboard(kPlayer.GamePlayer);
                    break;
                default:
                    Plugin.Instance.UI.SendKCHUD(kPlayer, BlueTeam, RedTeam);
                    break;
            }
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                return;
            }

            var kPlayer = GetKCPlayer(player.Player);

            Plugin.Instance.UI.ClearKCHUD(player);
            Plugin.Instance.UI.ClearPreEndingUI(player);
            Plugin.Instance.UI.ClearVoiceChatUI(player);
            OnStoppedTalking(player);

            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UI.ClearCountdownUI(player);
                kPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }
            else if (GamePhase == EGamePhase.WaitingForPlayers)
            {
                Plugin.Instance.UI.ClearWaitingForPlayersUI(player);
            }

            if (kPlayer != null)
            {
                kPlayer.Team.RemovePlayer(kPlayer.GamePlayer.SteamID);
                kPlayer.GamePlayer.OnGameLeft();
                Players.Remove(kPlayer);
                PlayersLookup.Remove(kPlayer.GamePlayer.SteamID);
            }

            Plugin.Instance.UI.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            var vPlayer = GetKCPlayer(player);
            if (vPlayer == null)
            {
                return;
            }

            if (GamePhase != EGamePhase.Started)
            {
                vPlayer.GamePlayer.OnDeath(CSteamID.Nil, 0);
                return;
            }

            if (vPlayer.GamePlayer.HasScoreboard)
            {
                vPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UI.HideKCLeaderboard(vPlayer.GamePlayer);
            }

            var victimKS = vPlayer.Killstreak;
            var updatedKiller = cause == EDeathCause.WATER ? vPlayer.GamePlayer.SteamID : (cause == EDeathCause.LANDMINE || cause == EDeathCause.SHRED ? (vPlayer.GamePlayer.LastDamager.Count > 0 ? vPlayer.GamePlayer.LastDamager.Pop() : killer) : killer);

            Logging.Debug($"Game player died, player name: {vPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");
            vPlayer.OnDeath(updatedKiller);
            vPlayer.GamePlayer.OnDeath(updatedKiller, Config.KC.FileData.RespawnSeconds);
            vPlayer.Team.OnDeath(vPlayer.GamePlayer.SteamID);
            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DB.IncreasePlayerDeathsAsync(vPlayer.GamePlayer.SteamID, 1));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                ItemManager.dropItem(new Item(vPlayer.Team.DogTagID, true), vPlayer.GamePlayer.Player.Player.transform.position, true, true, true);

                var kPlayer = GetKCPlayer(updatedKiller);
                if (kPlayer == null)
                {
                    Logging.Debug("Killer not found, returning");
                    return;
                }

                if (kPlayer.GamePlayer.SteamID == vPlayer.GamePlayer.SteamID)
                {
                    if (cause == EDeathCause.WATER)
                    {
                        OnKill(kPlayer.GamePlayer, vPlayer.GamePlayer, 0, kPlayer.Team.Info.KillFeedHexCode, vPlayer.Team.Info.KillFeedHexCode);
                    }
                    else if (cause == EDeathCause.LANDMINE || cause == EDeathCause.SHRED || cause == EDeathCause.GRENADE)
                    {
                        OnKill(kPlayer.GamePlayer, vPlayer.GamePlayer, 1, kPlayer.Team.Info.KillFeedHexCode, vPlayer.Team.Info.KillFeedHexCode);
                    }

                    Logging.Debug("Player killed themselves, returning");
                    return;
                }

                var questConditions = new Dictionary<EQuestCondition, int>
                {
                    { EQuestCondition.Map, Location.LocationID },
                    { EQuestCondition.Gamemode, (int)GameMode }
                };

                Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");

                if (vPlayer.GamePlayer.LastDamager.Count > 0 && vPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    vPlayer.GamePlayer.LastDamager.Pop();
                }

                if (vPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    var assister = GetKCPlayer(vPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        assister.Assists++;
                        assister.Score += Config.Points.FileData.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UI.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP, Plugin.Instance.Translate("Assist_Kill", vPlayer.GamePlayer.Player.CharacterName.ToUnrich()).ToRich());
                        }
                        ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DB.IncreasePlayerXPAsync(assister.GamePlayer.SteamID, Config.Medals.FileData.AssistKillXP));
                    }
                    vPlayer.GamePlayer.LastDamager.Clear();
                }

                var isFirstKill = Players[0].Kills == 0;
                kPlayer.Kills++;
                kPlayer.Score += Config.Points.FileData.KillPoints;

                int xpGained = 0;
                string xpText = "";
                ushort equipmentUsed = 0;
                var longshotRange = 0f;

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
                        if (equipment == (kPlayer.GamePlayer.ActiveLoadout.PrimarySkin?.SkinID ?? 0) || equipment == (kPlayer.GamePlayer.ActiveLoadout.Primary?.Gun?.GunID ?? 0))
                        {
                            questConditions.Add(EQuestCondition.GunType, (int)kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunType);
                            equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunID;
                            longshotRange = kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.LongshotRange;
                        }
                        else if (equipment == (kPlayer.GamePlayer.ActiveLoadout.SecondarySkin?.SkinID ?? 0) || equipment == (kPlayer.GamePlayer.ActiveLoadout.Secondary?.Gun?.GunID ?? 0))
                        {
                            questConditions.Add(EQuestCondition.GunType, (int)kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunType);
                            equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunID;
                            longshotRange = kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.LongshotRange;
                        }
                        else
                        {
                            equipmentUsed = equipment;
                        }
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
                    default:
                        break;
                }
                xpText += "\n";

                kPlayer.SetKillstreak(kPlayer.Killstreak + 1);
                questConditions.Add(EQuestCondition.TargetKS, kPlayer.Killstreak);
                if (kPlayer.MultipleKills == 0)
                {
                    kPlayer.SetMultipleKills(kPlayer.MultipleKills + 1);
                }
                else if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
                {
                    kPlayer.SetMultipleKills(kPlayer.MultipleKills + 1);
                    xpGained += Config.Medals.FileData.BaseXPMK + (kPlayer.MultipleKills * Config.Medals.FileData.IncreaseXPPerMK);
                    var multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
                    xpText += (multiKillText == $"Multiple_Kills_Show_{kPlayer.MultipleKills}" ? Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich() : multiKillText) + "\n";
                }
                else
                {
                    kPlayer.SetMultipleKills(1);
                }
                questConditions.Add(EQuestCondition.TargetMK, kPlayer.MultipleKills);

                if (victimKS > Config.Medals.FileData.ShutdownKillStreak)
                {
                    xpGained += Config.Medals.FileData.ShutdownXP;
                    xpText += Plugin.Instance.Translate("Shutdown_Kill").ToRich() + "\n";
                    Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Shutdown, questConditions);
                }

                if (kPlayer.PlayersKilled.ContainsKey(vPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[vPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[vPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                    {
                        xpGained += Config.Medals.FileData.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                        Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Shutdown, questConditions);
                    }
                }
                else
                {
                    kPlayer.PlayersKilled.Add(vPlayer.GamePlayer.SteamID, 1);
                }

                if (vPlayer.GamePlayer.SteamID == kPlayer.GamePlayer.LastKiller)
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

                if (cause == EDeathCause.GUN && (vPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
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
                kPlayer.GamePlayer.OnKilled(vPlayer.GamePlayer);

                if (equipmentUsed != 0)
                {
                    OnKill(kPlayer.GamePlayer, vPlayer.GamePlayer, equipmentUsed, kPlayer.Team.Info.KillFeedHexCode, vPlayer.Team.Info.KillFeedHexCode);
                }

                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Kill, questConditions);
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.MultiKill, questConditions);
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Killstreak, questConditions);
                if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                {
                    Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Headshots, questConditions);
                }
                Plugin.Instance.Quest.CheckQuest(vPlayer.GamePlayer, EQuestType.Death, questConditions);

                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    if (cause == EDeathCause.GUN && limb == ELimb.SKULL)
                    {
                        await Plugin.Instance.DB.IncreasePlayerHeadshotKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    else
                    {
                        await Plugin.Instance.DB.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, xpGained);
                    if ((kPlayer.GamePlayer.ActiveLoadout.Primary != null && kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunID == equipmentUsed) || (kPlayer.GamePlayer.ActiveLoadout.Secondary != null && kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunID == equipmentUsed))
                    {
                        await Plugin.Instance.DB.IncreasePlayerGunXPAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, xpGained);
                        await Plugin.Instance.DB.IncreasePlayerGunKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                    else if (kPlayer.GamePlayer.ActiveLoadout.Lethal != null && kPlayer.GamePlayer.ActiveLoadout.Lethal.Gadget.GadgetID == equipmentUsed)
                    {
                        await Plugin.Instance.DB.IncreasePlayerGadgetKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                    else if (kPlayer.GamePlayer.ActiveLoadout.Killstreaks.Select(k => k.Killstreak.KillstreakID).Contains(equipmentUsed))
                    {
                        await Plugin.Instance.DB.IncreasePlayerKillstreakKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                    else if (kPlayer.GamePlayer.ActiveLoadout.Knife != null && kPlayer.GamePlayer.ActiveLoadout.Knife.Knife.KnifeID == equipmentUsed)
                    {
                        await Plugin.Instance.DB.IncreasePlayerKnifeKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                });
            });
        }

        public override void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            var player = GetKCPlayer(parameters.player);
            if (player == null)
            {
                return;
            }

            parameters.applyGlobalArmorMultiplier = IsHardcore;
            if (GamePhase != EGamePhase.Started)
            {
                Logging.Debug($"{player.GamePlayer.Player.CharacterName} got damaged but damage got ignored due to game not started yet");
                shouldAllow = false;
                return;
            }

            if (player.GamePlayer.HasSpawnProtection)
            {
                Logging.Debug($"{player.GamePlayer.Player.CharacterName} got damaged but damage got ignored due to spawn protection, Is Timer Enabled: {player.GamePlayer.m_RemoveSpawnProtection.Enabled}");
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

            parameters.damage -= (player.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageReducePerkName, out LoadoutPerk damageReducerPerk) ? ((float)damageReducerPerk.Perk.SkillLevel / 100) : 0f) * parameters.damage;

            player.GamePlayer.OnDamaged(parameters.killer);

            var kPlayer = GetKCPlayer(parameters.killer);
            if (kPlayer == null)
            {
                return;
            }

            if (kPlayer.Team == player.Team && kPlayer != player)
            {
                Logging.Debug($"{player.GamePlayer.Player.CharacterName} got damaged by {kPlayer.GamePlayer.Player.CharacterName} but damage got ignored due to player being same team");
                shouldAllow = false;
                return;
            }

            parameters.damage += (kPlayer.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageIncreasePerkName, out LoadoutPerk damageIncreaserPerk) ? ((float)damageIncreaserPerk.Perk.SkillLevel / 100) : 0f) * parameters.damage;

            if (kPlayer.GamePlayer.HasSpawnProtection)
            {
                Logging.Debug($"{kPlayer.GamePlayer.Player.CharacterName} damaged someone but had spawn protection, turning it off");
                kPlayer.GamePlayer.m_RemoveSpawnProtection.Stop();
                kPlayer.GamePlayer.HasSpawnProtection = false;
            }
        }

        public override void OnPlayerRevived(UnturnedPlayer player)
        {
            var kPlayer = GetKCPlayer(player);
            if (kPlayer == null)
            {
                return;
            }

            kPlayer.GamePlayer.OnRevived(kPlayer.Team.Info.TeamKits[UnityEngine.Random.Range(0, kPlayer.Team.Info.TeamKits.Count)], kPlayer.Team.Info.TeamGloves);
        }

        public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition, ref float yaw)
        {
            var kPlayer = GetKCPlayer(player.Player);
            if (kPlayer == null)
            {
                return;
            }

            if (!SpawnPoints.TryGetValue(kPlayer.Team.SpawnPoint, out var spawnPoints))
            {
                return;
            }

            if (spawnPoints.Count == 0)
            {
                return;
            }

            var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            respawnPosition = spawnPoint.GetSpawnPoint();
            yaw = spawnPoint.Yaw;

            player.GiveSpawnProtection(Config.KC.FileData.SpawnProtectionSeconds);
        }

        public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
        {
            var kPlayer = GetKCPlayer(player.Player);
            if (kPlayer == null)
            {
                return;
            }

            if (text.Substring(0, 1) == "/")
            {
                return;
            }

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

                var iconLink = Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "";
                var updatedText = $"[{Utility.ToFriendlyName(chatMode)}] <color={Utility.GetLevelColor(player.Data.Level)}>[{player.Data.Level}]</color> <color={kPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={kPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

                var loopPlayers = chatMode == EChatMode.GLOBAL ? Players : Players.Where(k => k.Team == kPlayer.Team);
                foreach (var reciever in loopPlayers)
                {
                    ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: player.Data.AvatarLink, useRichTextFormatting: true);
                }
            });
        }

        public override void OnVoiceChatUpdated(GamePlayer player)
        {
            if (GamePhase == EGamePhase.Ending)
            {
                SendVoiceChat(Players.Select(k => k.GamePlayer).ToList(), false);
                return;
            }

            var kPlayer = GetKCPlayer(player.Player);
            SendVoiceChat(Players.Where(k => k.Team == kPlayer.Team).Select(k => k.GamePlayer).ToList(), true);
        }

        public override void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            if (P == null)
            {
                return;
            }

            var kPlayer = GetKCPlayer(player.CSteamID);
            if (kPlayer == null)
            {
                return;
            }

            if (GamePhase == EGamePhase.Ending)
            {
                return;
            }

            var otherTeam = kPlayer.Team.TeamID == (byte)ETeam.Blue ? RedTeam : BlueTeam;
            var questConditions = new Dictionary<EQuestCondition, int>
            {
                { EQuestCondition.Map, Location.LocationID },
                { EQuestCondition.Gamemode, (int)GameMode }
            };

            var xpGained = 0;
            var xpText = "";

            if (kPlayer.Team.DogTagID == P.item.id)
            {
                kPlayer.Score += Config.Points.FileData.KillDeniedPoints;
                kPlayer.XP += Config.Medals.FileData.KillDeniedXP;
                kPlayer.KillsDenied++;
                xpGained += Config.Medals.FileData.KillDeniedXP;
                xpText += Plugin.Instance.Translate("Kill_Denied").ToRich() + "\n";
                Plugin.Instance.UI.SendKillConfirmedSound(kPlayer.GamePlayer);
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, Config.Medals.FileData.KillDeniedXP);
                    await Plugin.Instance.DB.IncreasePlayerKillsDeniedAsync(kPlayer.GamePlayer.SteamID, 1);
                });

            }
            else if (P.item.id == otherTeam.DogTagID)
            {
                kPlayer.Score += Config.Points.FileData.KillConfirmedPoints;
                kPlayer.XP += Config.Medals.FileData.KillConfirmedXP;
                kPlayer.KillsConfirmed++;
                kPlayer.Team.Score++;
                xpGained += Config.Medals.FileData.KillConfirmedXP;
                xpText += Plugin.Instance.Translate("Kill_Confirmed").ToRich() + "\n";
                Plugin.Instance.UI.SendKillDeniedSound(kPlayer.GamePlayer);
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, Config.Medals.FileData.KillConfirmedXP);
                    await Plugin.Instance.DB.IncreasePlayerKillsConfirmedAsync(kPlayer.GamePlayer.SteamID, 1);
                });

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    foreach (var ply in Players)
                    {
                        Plugin.Instance.UI.UpdateKCScore(ply, kPlayer.Team);
                    }
                    if (kPlayer.Team.Score == Config.KC.FileData.ScoreLimit)
                    {
                        Plugin.Instance.StartCoroutine(GameEnd(kPlayer.Team));
                    }
                });
            }
            else
            {
                return;
            }

            kPlayer.CollectorTags += 1;
            if (kPlayer.CollectorTags == Config.Medals.FileData.CollectorTags)
            {
                kPlayer.CollectorTags = 0;

                xpGained += Config.Medals.FileData.CollectorXP;
                xpText += Plugin.Instance.Translate("Collector").ToRich();
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(player.CSteamID, Config.Medals.FileData.CollectorXP);
                });

                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Collector, questConditions));
            }

            Plugin.Instance.UI.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Dogtags, questConditions));
            player.Player.inventory.removeItem((byte)inventoryGroup, inventoryIndex);
        }

        public void GiveLoadout(KCPlayer player)
        {
            player.GamePlayer.Player.Player.inventory.ClearInventory();
            Plugin.Instance.Loadout.GiveLoadout(player.GamePlayer, player.Team.Info.TeamKits[UnityEngine.Random.Range(0, player.Team.Info.TeamKits.Count)], player.Team.Info.TeamGloves);
        }

        public void SpawnPlayer(KCPlayer player)
        {
            if (!SpawnPoints.TryGetValue(player.Team.SpawnPoint, out var spawnPoints))
            {
                return;
            }

            if (spawnPoints.Count == 0)
            {
                return;
            }

            var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), spawnPoint.Yaw);
            player.GamePlayer.GiveSpawnProtection(Config.KC.FileData.SpawnProtectionSeconds);
        }

        public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
        {
            var kPlayer = GetKCPlayer(player.Player);
            if (kPlayer == null)
            {
                return;
            }

            if (throwable.equippedThrowableAsset.id == (player.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0))
            {
                player.UsedLethal();
            }
            else if (throwable.equippedThrowableAsset.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0))
            {
                player.UsedTactical();
            }
        }

        public override void PlayerConsumeableUsed(GamePlayer player, ItemConsumeableAsset consumeableAsset)
        {
            if (IsPlayerIngame(player.SteamID) && consumeableAsset.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0))
            {
                player.UsedTactical();
            }
        }

        public override void PlayerBarricadeSpawned(GamePlayer player, BarricadeDrop drop)
        {
            var kPlayer = GetKCPlayer(player.Player);
            if (kPlayer == null)
            {
                return;
            }

            if (drop.asset.id == (player.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0))
            {
                player.UsedLethal();
            }
            else if (drop.asset.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0))
            {
                player.UsedTactical();
            }
        }

        public override void PlayerChangeFiremode(GamePlayer player)
        {
            KCPlayer kPlayer = GetKCPlayer(player.Player);
            if (kPlayer == null)
            {
                return;
            }

            if (GamePhase == EGamePhase.Ending)
            {
                return;
            }

            if (player.ScoreboardCooldown > DateTime.UtcNow)
            {
                return;
            }
            player.ScoreboardCooldown = DateTime.UtcNow.AddSeconds(0.5);

            if (kPlayer.GamePlayer.HasScoreboard)
            {
                kPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UI.HideKCLeaderboard(kPlayer.GamePlayer);
            }
            else
            {
                kPlayer.GamePlayer.HasScoreboard = true;
                KCTeam wonTeam;
                if (BlueTeam.Score > RedTeam.Score)
                {
                    wonTeam = BlueTeam;
                }
                else if (RedTeam.Score > BlueTeam.Score)
                {
                    wonTeam = RedTeam;
                }
                else
                {
                    wonTeam = new KCTeam(this, -1, true, 0, new TeamInfo());
                }
                Plugin.Instance.UI.SetupKCLeaderboard(kPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
                Plugin.Instance.UI.ShowKCLeaderboard(kPlayer.GamePlayer);
            }
        }

        public override void PlayerStanceChanged(PlayerStance obj)
        {
            var kPlayer = GetKCPlayer(obj.player);
            if (kPlayer == null)
            {
                return;
            }

            kPlayer.GamePlayer.OnStanceChanged(obj.stance);
        }

        public override void PlayerEquipmentChanged(GamePlayer player)
        {
            if (IsPlayerIngame(player.SteamID) && GamePhase != EGamePhase.Starting)
            {
                player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, true);
            }
        }

        public override void PlayerAimingChanged(GamePlayer player, bool isAiming)
        {
            if (IsPlayerIngame(player.SteamID) && GamePhase != EGamePhase.Starting)
            {
                player.GiveMovement(isAiming, false, false);
            }
        }

        private void SpawnSwitch(object sender, System.Timers.ElapsedEventArgs e)
        {
            SwitchSpawn();
        }

        public void SwitchSpawn()
        {
            if (m_SpawnSwitcher.Enabled)
            {
                m_SpawnSwitcher.Stop();
            }

            if (RedTeam.m_CheckSpawnSwitch.Enabled)
            {
                RedTeam.m_CheckSpawnSwitch.Stop();
                RedTeam.SpawnThreshold = 0;
            }

            if (BlueTeam.m_CheckSpawnSwitch.Enabled)
            {
                BlueTeam.m_CheckSpawnSwitch.Stop();
                BlueTeam.SpawnThreshold = 0;
            }

            var keys = SpawnPoints.Keys.ToList();
            if (keys.Count == 0)
            {
                return;
            }

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

            m_SpawnSwitcher.Start();
        }

        public KCPlayer GetKCPlayer(CSteamID steamID)
        {
            return PlayersLookup.TryGetValue(steamID, out KCPlayer tPlayer) ? tPlayer : null;
        }

        public KCPlayer GetKCPlayer(UnturnedPlayer player)
        {
            return PlayersLookup.TryGetValue(player.CSteamID, out KCPlayer tPlayer) ? tPlayer : null;
        }

        public KCPlayer GetKCPlayer(Player player)
        {
            return PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out KCPlayer tPlayer) ? tPlayer : null;
        }

        public override bool IsPlayerIngame(CSteamID steamID)
        {
            return PlayersLookup.ContainsKey(steamID);
        }

        public override int GetPlayerCount()
        {
            return Players.Count;
        }

        public override List<GamePlayer> GetPlayers()
        {
            return Players.Select(k => k.GamePlayer).ToList();
        }

        public override void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow)
        {

        }

        public override bool IsPlayerCarryingFlag(GamePlayer player)
        {
            return false;
        }
    }
}

