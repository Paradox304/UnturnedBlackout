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
using System.Threading.Tasks;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.TDM;
using Timer = System.Timers.Timer;

namespace UnturnedBlackout.GameTypes
{
    public class TDMGame : Game
    {
        public Dictionary<int, List<TDMSpawnPoint>> SpawnPoints { get; set; }

        public List<TDMPlayer> Players { get; set; }
        public Dictionary<CSteamID, TDMPlayer> PlayersLookup { get; set; }

        public TDMTeam BlueTeam { get; set; }
        public TDMTeam RedTeam { get; set; }

        public Coroutine GameStarter { get; set; }
        public Coroutine GameEnder { get; set; }
        //public Timer m_SpawnSwitcher { get; set; }
        public Coroutine SpawnSwitcher { get; set; }

        public uint Frequency { get; set; }

        public TDMGame(ArenaLocation location, bool isHardcore) : base(EGameType.TDM, location, isHardcore)
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
            Players = new List<TDMPlayer>();
            PlayersLookup = new Dictionary<CSteamID, TDMPlayer>();

            var blueTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
            var redTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

            BlueTeam = new TDMTeam(this, (byte)ETeam.Blue, false, blueTeamInfo);
            RedTeam = new TDMTeam(this, (byte)ETeam.Red, false, redTeamInfo);
            Frequency = Utility.GetFreeFrequency();

            /*
            m_SpawnSwitcher = new Timer(Config.Base.FileData.SpawnSwitchSeconds * 1000);
            m_SpawnSwitcher.Elapsed += SpawnSwitch;
            */
        }

        public IEnumerator StartGame()
        {
            TaskDispatcher.QueueOnMainThread(() => CleanMap());
            GamePhase = EGamePhase.Starting;
            foreach (var player in Players)
            {
                Plugin.Instance.UI.ClearWaitingForPlayersUI(player.GamePlayer);
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UI.ShowCountdownUI(player.GamePlayer);
                SpawnPlayer(player);
            }

            for (int seconds = Config.TDM.FileData.StartSeconds; seconds >= 0; seconds--)
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
                Plugin.Instance.UI.SendTDMHUD(player, BlueTeam, RedTeam);
                Plugin.Instance.UI.ClearCountdownUI(player.GamePlayer);
            }

            SpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.TDM.FileData.EndSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                foreach (var player in Players)
                {
                    Plugin.Instance.UI.UpdateTDMTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
                }
            }

            TDMTeam wonTeam;
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
                wonTeam = new TDMTeam(this, -1, true, new TeamInfo());
            }
            Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
        }

        public IEnumerator GameEnd(TDMTeam wonTeam)
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
                    continue;
                }
                roundEndCases.Add((roundEndCasePlayer, @case));
                Task.Run(async () =>
                {
                    await Plugin.Instance.DB.IncreasePlayerCaseAsync(roundEndCasePlayer.SteamID, @case.CaseID, 1);
                });
            }

            var summaries = new Dictionary<GamePlayer, MatchEndSummary>();
            foreach (var player in Players)
            {
                Plugin.Instance.UI.ClearTDMHUD(player.GamePlayer);
                Plugin.Instance.UI.ClearMidgameLoadoutUI(player.GamePlayer);
                if (player.GamePlayer.Player.Player.life.isDead)
                {
                    player.GamePlayer.Player.Player.life.ServerRespawn(false);
                }
                Plugin.Instance.UI.RemoveKillCard(player.GamePlayer);

                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UI.HideTDMLeaderboard(player.GamePlayer);
                }

                var summary = new MatchEndSummary(player.GamePlayer, player.XP, player.StartingLevel, player.StartingXP, player.Kills, player.Deaths, player.Assists, player.HighestKillstreak, player.HighestMK, player.StartTime, GameMode, player.Team == wonTeam);
                summaries.Add(player.GamePlayer, summary);
                Task.Run(async () =>
                {
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(player.GamePlayer.SteamID, summary.PendingXP);
                    await Plugin.Instance.DB.IncreasePlayerCreditsAsync(player.GamePlayer.SteamID, summary.PendingCredits);
                    await Plugin.Instance.DB.IncreasePlayerBPXPAsync(player.GamePlayer.SteamID, summary.BattlepassXP + summary.BattlepassBonusXP);
                });

                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(player.GamePlayer, EQuestType.FinishMatch, new Dictionary<EQuestCondition, int> { { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode }, { EQuestCondition.WinKills, player.Kills } }));
                if (player.Team == wonTeam)
                {
                    TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(player.GamePlayer, EQuestType.Win, new Dictionary<EQuestCondition, int> { { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode }, { EQuestCondition.WinKills, player.Kills } }));
                }

                Plugin.Instance.UI.SetupPreEndingUI(player.GamePlayer, EGameType.TDM, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName);
                player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UI.SetupTDMLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false, IsHardcore);
                CleanMap();
            });
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UI.ShowTDMLeaderboard(player.GamePlayer);
            }

            if (roundEndCases.Count > 0)
            {
                Plugin.Instance.StartCoroutine(Plugin.Instance.UI.SetupRoundEndDrops(Players.Select(k => k.GamePlayer).ToList(), roundEndCases, 0));
            }

            yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds);
            foreach (var player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.Game.SendPlayerToLobby(player.GamePlayer.Player, summaries.TryGetValue(player.GamePlayer, out MatchEndSummary pendingSummary) ? pendingSummary : null);
            }

            Players = new List<TDMPlayer>();
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
                GamePhase = EGamePhase.Ended;
                Plugin.Instance.Game.EndGame(this);
                Plugin.Instance.Game.StartGame(location, gameMode.Item1, gameMode.Item2);
            }
        }

        public override IEnumerator AddPlayerToGame(GamePlayer player)
        {
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                yield break;
            }

            player.OnGameJoined(this);
            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;
            TDMPlayer tPlayer = new(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(tPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, tPlayer);

            Plugin.Instance.UI.OnGameCountUpdated(this);
            Plugin.Instance.UI.SendLoadingUI(player.Player, true, GameMode, Location);
            for (int seconds = 1; seconds <= 5; seconds++)
            {
                yield return new WaitForSeconds(1);
                Plugin.Instance.UI.UpdateLoadingBar(player.Player, new string('　', Math.Min(96, seconds * 96 / 5)));
            }
            var currentPos = player.Player.Position;
            player.Player.Player.teleportToLocationUnsafe(new Vector3(currentPos.x, currentPos.y + 100, currentPos.z), 0);
            GiveLoadout(tPlayer);
            Plugin.Instance.UI.SendPreEndingUI(tPlayer.GamePlayer);
            SpawnPlayer(tPlayer);
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
                            if (ply == tPlayer)
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
                    TDMTeam wonTeam;
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
                        wonTeam = new TDMTeam(this, -1, true, new TeamInfo());
                    }
                    Plugin.Instance.UI.SetupTDMLeaderboard(tPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
                    Plugin.Instance.UI.ShowTDMLeaderboard(tPlayer.GamePlayer);
                    break;
                default:
                    Plugin.Instance.UI.SendTDMHUD(tPlayer, BlueTeam, RedTeam);
                    break;
            }
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            if (!PlayersLookup.ContainsKey(player.SteamID))
            {
                return;
            }

            var tPlayer = GetTDMPlayer(player.Player);

            Plugin.Instance.UI.ClearTDMHUD(player);
            Plugin.Instance.UI.ClearPreEndingUI(player);
            Plugin.Instance.UI.ClearVoiceChatUI(player);
            Plugin.Instance.UI.ClearKillstreakUI(player);
            OnStoppedTalking(player);

            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UI.ClearCountdownUI(player);
                tPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }
            else if (GamePhase == EGamePhase.WaitingForPlayers)
            {
                Plugin.Instance.UI.ClearWaitingForPlayersUI(player);
                foreach (var ply in Players)
                {
                    if (ply == tPlayer)
                    {
                        continue;
                    }

                    Plugin.Instance.UI.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count - 1, Location.GetMinPlayers(GameMode));
                }
            }

            tPlayer.Team.RemovePlayer(tPlayer.GamePlayer.SteamID);
            tPlayer.GamePlayer.OnGameLeft();
            Players.Remove(tPlayer);
            PlayersLookup.Remove(tPlayer.GamePlayer.SteamID);

            Plugin.Instance.UI.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            var tPlayer = GetTDMPlayer(player);
            if (tPlayer == null)
            {
                return;
            }

            if (cause == EDeathCause.SUICIDE)
            {
                RemovePlayerFromGame(tPlayer.GamePlayer);
                return;
            }

            if (tPlayer.GamePlayer.HasScoreboard)
            {
                tPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UI.HideTDMLeaderboard(tPlayer.GamePlayer);
            }

            var victimKS = tPlayer.Killstreak;
            var updatedKiller = cause == EDeathCause.WATER ? tPlayer.GamePlayer.SteamID : (cause == EDeathCause.LANDMINE || cause == EDeathCause.SHRED ? (tPlayer.GamePlayer.LastDamager.Count > 0 ? tPlayer.GamePlayer.LastDamager.Pop() : killer) : killer);

            Logging.Debug($"Game player died, player name: {tPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");
            tPlayer.OnDeath(updatedKiller);
            tPlayer.GamePlayer.OnDeath(updatedKiller, Config.TDM.FileData.RespawnSeconds);
            tPlayer.Team.OnDeath(tPlayer.GamePlayer.SteamID);

            Task.Run(async () => await Plugin.Instance.DB.IncreasePlayerDeathsAsync(tPlayer.GamePlayer.SteamID, 1));

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

                var questConditions = new Dictionary<EQuestCondition, int>
                {
                    { EQuestCondition.Map, Location.LocationID },
                    { EQuestCondition.Gamemode, (int)GameMode }
                };

                Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");

                if (tPlayer.GamePlayer.LastDamager.Count > 0 && tPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    tPlayer.GamePlayer.LastDamager.Pop();
                }

                if (tPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    var assister = GetTDMPlayer(tPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        assister.Assists++;
                        assister.Score += Config.Points.FileData.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UI.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP, Plugin.Instance.Translate("Assist_Kill", tPlayer.GamePlayer.Player.CharacterName.ToUnrich()));
                        }
                        Task.Run(async () => await Plugin.Instance.DB.IncreasePlayerXPAsync(assister.GamePlayer.SteamID, Config.Medals.FileData.AssistKillXP));
                    }
                    tPlayer.GamePlayer.LastDamager.Clear();
                }

                var isFirstKill = Players[0].Kills == 0;
                kPlayer.Kills++;
                kPlayer.Team.Score++;
                kPlayer.Score += Config.Points.FileData.KillPoints;

                int xpGained = 0;
                string xpText = "";
                ushort equipmentUsed = 0;
                var longshotRange = 0f;

                var usedKillstreak = kPlayer.GamePlayer.HasKillstreakActive;
                var killstreakID = kPlayer.GamePlayer.ActiveKillstreak?.Killstreak?.KillstreakID ?? 0;

                if (usedKillstreak)
                {
                    var info = kPlayer.GamePlayer.ActiveKillstreak.Killstreak.KillstreakInfo;
                    xpGained += info.MedalXP;
                    xpText += info.MedalName;
                    equipmentUsed += info.ItemID;
                    questConditions.Add(EQuestCondition.Killstreak, equipmentUsed);
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

                if (kPlayer.PlayersKilled.ContainsKey(tPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[tPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[tPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                    {
                        xpGained += Config.Medals.FileData.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                        Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Domination, questConditions);
                    }
                }
                else
                {
                    kPlayer.PlayersKilled.Add(tPlayer.GamePlayer.SteamID, 1);
                }

                if (tPlayer.GamePlayer.SteamID == kPlayer.GamePlayer.LastKiller)
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

                if (!usedKillstreak && cause == EDeathCause.GUN && (tPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
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
                kPlayer.GamePlayer.OnKilled(tPlayer.GamePlayer);

                foreach (var ply in Players)
                {
                    Plugin.Instance.UI.UpdateTDMScore(ply, kPlayer.Team);
                }
                if (kPlayer.Team.Score == Config.TDM.FileData.ScoreLimit)
                {
                    Plugin.Instance.StartCoroutine(GameEnd(kPlayer.Team));
                }
                if (equipmentUsed != 0)
                {
                    OnKill(kPlayer.GamePlayer, tPlayer.GamePlayer, equipmentUsed, kPlayer.Team.Info.KillFeedHexCode, tPlayer.Team.Info.KillFeedHexCode);
                }

                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Kill, questConditions);
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.MultiKill, questConditions);
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Killstreak, questConditions);
                if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                {
                    Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Headshots, questConditions);
                }
                Plugin.Instance.Quest.CheckQuest(tPlayer.GamePlayer, EQuestType.Death, questConditions);

                Task.Run(async () =>
                {
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, xpGained);
                    if (cause == EDeathCause.GUN && limb == ELimb.SKULL)
                    {
                        await Plugin.Instance.DB.IncreasePlayerHeadshotKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    else
                    {
                        await Plugin.Instance.DB.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }

                    if (usedKillstreak)
                    {
                        await Plugin.Instance.DB.IncreasePlayerKillstreakKillsAsync(kPlayer.GamePlayer.SteamID, killstreakID, 1);
                    }
                    else if ((kPlayer.GamePlayer.ActiveLoadout.Primary != null && kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunID == equipmentUsed) || (kPlayer.GamePlayer.ActiveLoadout.Secondary != null && kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunID == equipmentUsed))
                    {
                        await Plugin.Instance.DB.IncreasePlayerGunXPAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, xpGained);
                        await Plugin.Instance.DB.IncreasePlayerGunKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                    else if (kPlayer.GamePlayer.ActiveLoadout.Lethal != null && kPlayer.GamePlayer.ActiveLoadout.Lethal.Gadget.GadgetID == equipmentUsed)
                    {
                        await Plugin.Instance.DB.IncreasePlayerGadgetKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
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
            var player = GetTDMPlayer(parameters.player);
            if (player == null)
            {
                return;
            }

            parameters.applyGlobalArmorMultiplier = IsHardcore;
            if (GamePhase != EGamePhase.Started)
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

            parameters.damage -= (player.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageReducePerkName, out LoadoutPerk damageReducerPerk) ? ((float)damageReducerPerk.Perk.SkillLevel / 100) : 0f) * parameters.damage;

            player.GamePlayer.OnDamaged(parameters.killer);

            var kPlayer = GetTDMPlayer(parameters.killer);
            if (kPlayer == null)
            {
                return;
            }

            if (kPlayer.Team == player.Team && kPlayer != player)
            {
                shouldAllow = false;
                return;
            }

            parameters.damage += (kPlayer.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageIncreasePerkName, out LoadoutPerk damageIncreaserPerk) ? ((float)damageIncreaserPerk.Perk.SkillLevel / 100) : 0f) * parameters.damage;

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
            {
                return;
            }

            tPlayer.GamePlayer.OnRevived(tPlayer.Team.Info.TeamKits[UnityEngine.Random.Range(0, tPlayer.Team.Info.TeamKits.Count)], tPlayer.Team.Info.TeamGloves);
        }

        public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition, ref float yaw)
        {
            var tPlayer = GetTDMPlayer(player.Player);
            if (tPlayer == null)
            {
                return;
            }

            if (!SpawnPoints.TryGetValue(tPlayer.Team.SpawnPoint, out var spawnPoints))
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

            player.GiveSpawnProtection(Config.TDM.FileData.SpawnProtectionSeconds);
        }

        public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
        {
            var tPlayer = GetTDMPlayer(player.Player);
            if (tPlayer == null)
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
                var updatedText = $"[{Utility.ToFriendlyName(chatMode)}] <color={Utility.GetLevelColor(player.Data.Level)}>[{player.Data.Level}]</color> <color={tPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={tPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

                var loopPlayers = chatMode == EChatMode.GLOBAL ? Players : Players.Where(k => k.Team == tPlayer.Team);
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

            var tPlayer = GetTDMPlayer(player.Player);
            SendVoiceChat(Players.Where(k => k.Team == tPlayer.Team).Select(k => k.GamePlayer).ToList(), true);
        }

        public void GiveLoadout(TDMPlayer player)
        {
            player.GamePlayer.Player.Player.inventory.ClearInventory();
            Plugin.Instance.Loadout.GiveLoadout(player.GamePlayer, player.Team.Info.TeamKits[UnityEngine.Random.Range(0, player.Team.Info.TeamKits.Count)], player.Team.Info.TeamGloves);
        }

        public void SpawnPlayer(TDMPlayer player)
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
            player.GamePlayer.GiveSpawnProtection(Config.TDM.FileData.SpawnProtectionSeconds);
        }

        public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
        {
            var tPlayer = GetTDMPlayer(player.Player);
            if (tPlayer == null)
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
            var tPlayer = GetTDMPlayer(player.Player);
            if (tPlayer == null)
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
            TDMPlayer tPlayer = GetTDMPlayer(player.Player);
            if (tPlayer == null)
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

            if (tPlayer.GamePlayer.HasScoreboard)
            {
                tPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UI.HideTDMLeaderboard(tPlayer.GamePlayer);
            }
            else
            {
                tPlayer.GamePlayer.HasScoreboard = true;
                TDMTeam wonTeam;
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
                    wonTeam = new TDMTeam(this, -1, true, new TeamInfo());
                }
                Plugin.Instance.UI.SetupTDMLeaderboard(tPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
                Plugin.Instance.UI.ShowTDMLeaderboard(tPlayer.GamePlayer);
            }
        }

        public override void PlayerStanceChanged(PlayerStance obj)
        {
            var tPlayer = GetTDMPlayer(obj.player);
            if (tPlayer == null)
            {
                return;
            }

            tPlayer.GamePlayer.OnStanceChanged(obj.stance);
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

        /*
        private void SpawnSwitch(object sender, System.Timers.ElapsedEventArgs e)
        {
            SwitchSpawn();
        }
        */

        public IEnumerator SpawnSwitch()
        {
            yield return new WaitForSeconds(Config.Base.FileData.SpawnSwitchSeconds);
            SwitchSpawn();
        }

        public void SwitchSpawn()
        {
            SpawnSwitcher.Stop();

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

            SpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
        }

        public TDMPlayer GetTDMPlayer(CSteamID steamID)
        {
            return PlayersLookup.TryGetValue(steamID, out TDMPlayer tPlayer) ? tPlayer : null;
        }

        public TDMPlayer GetTDMPlayer(UnturnedPlayer player)
        {
            return PlayersLookup.TryGetValue(player.CSteamID, out TDMPlayer tPlayer) ? tPlayer : null;
        }

        public TDMPlayer GetTDMPlayer(Player player)
        {
            return PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out TDMPlayer tPlayer) ? tPlayer : null;
        }

        public override bool IsPlayerIngame(CSteamID steamID)
        {
            return PlayersLookup.ContainsKey(steamID);
        }

        public override int GetPlayerCount()
        {
            return Players.Count;
        }

        public override void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {

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
