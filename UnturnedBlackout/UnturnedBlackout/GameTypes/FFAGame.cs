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
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.GameTypes
{
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
            Players = new List<FFAPlayer>();
            PlayersLookup = new Dictionary<CSteamID, FFAPlayer>();
            UnavailableSpawnPoints = new List<FFASpawnPoint>();
            Frequency = Utility.GetFreeFrequency();
        }

        public IEnumerator StartGame()
        {
            TaskDispatcher.QueueOnMainThread(() => CleanMap());
            GamePhase = EGamePhase.Starting;
            Plugin.Instance.UI.OnGameUpdated();
            foreach (FFAPlayer player in Players)
            {
                Plugin.Instance.UI.ClearWaitingForPlayersUI(player.GamePlayer);
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UI.ShowCountdownUI(player.GamePlayer);
                SpawnPlayer(player);
            }

            for (int seconds = Config.FFA.FileData.StartSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                foreach (FFAPlayer player in Players)
                {
                    Plugin.Instance.UI.SendCountdownSeconds(player.GamePlayer, seconds);
                }
            }
            GamePhase = EGamePhase.Started;
            Plugin.Instance.UI.OnGameUpdated();
            foreach (FFAPlayer player in Players)
            {
                player.GamePlayer.GiveMovement(player.GamePlayer.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, false);

                Plugin.Instance.UI.SendFFAHUD(player.GamePlayer);
                Plugin.Instance.UI.ClearCountdownUI(player.GamePlayer);
                Plugin.Instance.UI.UpdateFFATopUI(player, Players);
                player.StartTime = DateTime.UtcNow;
            }

            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.FFA.FileData.EndSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                foreach (FFAPlayer player in Players)
                {
                    Plugin.Instance.UI.UpdateFFATimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
                }
            }

            Plugin.Instance.StartCoroutine(GameEnd());
        }

        public IEnumerator GameEnd()
        {
            if (GameEnder != null)
            {
                Plugin.Instance.StopCoroutine(GameEnder);
            }

            GamePhase = EGamePhase.Ending;
            Plugin.Instance.UI.OnGameUpdated();
            Dictionary<GamePlayer, MatchEndSummary> summaries = new();

            DateTime endTime = DateTime.UtcNow;
            List<GamePlayer> roundEndCasesPlayers = new();
            foreach (FFAPlayer player in Players)
            {
                int totalMinutesPlayed = (int)(endTime - player.StartTime).TotalMinutes;
                if (totalMinutesPlayed < Config.RoundEndCases.FileData.MinimumMinutesPlayed || player.Kills == 0)
                {
                    continue;
                }

                int chance = Config.RoundEndCases.FileData.Chance * totalMinutesPlayed;
                if (UnityEngine.Random.Range(1, 101) > chance)
                {
                    continue;
                }
                roundEndCasesPlayers.Add(player.GamePlayer);
                if (roundEndCasesPlayers.Count == 8) break;
            }

            List<(GamePlayer, Case)> roundEndCases = new();
            foreach (GamePlayer roundEndCasePlayer in roundEndCasesPlayers)
            {
                Case @case = GetRandomRoundEndCase();
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

            for (int index = 0; index < Players.Count; index++)
            {
                FFAPlayer player = Players[index];
                Plugin.Instance.UI.ClearFFAHUD(player.GamePlayer);
                Plugin.Instance.UI.ClearMidgameLoadoutUI(player.GamePlayer);
                if (player.GamePlayer.Player.Player.life.isDead)
                {
                    player.GamePlayer.Player.Player.life.ServerRespawn(false);
                }
                Plugin.Instance.UI.RemoveKillCard(player.GamePlayer);
                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UI.HideFFALeaderboard(player.GamePlayer);
                }
                Plugin.Instance.UI.SetupPreEndingUI(player.GamePlayer, EGameType.FFA, index == 0, 0, 0, "", "");
                MatchEndSummary summary = new(player.GamePlayer, player.XP, player.StartingLevel, player.StartingXP, player.Kills, player.Deaths, player.Assists, player.HighestKillstreak, player.HighestMK, player.StartTime, GameMode, index == 0);
                summaries.Add(player.GamePlayer, summary);
                Task.Run(async () =>
                {
                    await Plugin.Instance.DB.IncreasePlayerXPAsync(player.GamePlayer.SteamID, summary.PendingXP);
                    await Plugin.Instance.DB.IncreasePlayerCreditsAsync(player.GamePlayer.SteamID, summary.PendingCredits);
                    await Plugin.Instance.DB.IncreasePlayerBPXPAsync(player.GamePlayer.SteamID, summary.BattlepassXP + summary.BattlepassBonusXP);
                });

                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(player.GamePlayer, EQuestType.FinishMatch, new Dictionary<EQuestCondition, int> { { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode }, { EQuestCondition.WinKills, player.Kills } }));
                if (index == 0)
                {
                    TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(player.GamePlayer, EQuestType.Win, new Dictionary<EQuestCondition, int> { { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode }, { EQuestCondition.WinKills, player.Kills } }));
                }
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UI.SetupFFALeaderboard(Players, Location, false, IsHardcore);
                CleanMap();
            });
            yield return new WaitForSeconds(5);
            foreach (FFAPlayer player in Players)
            {
                Plugin.Instance.UI.ShowFFALeaderboard(player.GamePlayer);
            }

            if (roundEndCases.Count > 0)
            {
                Plugin.Instance.StartCoroutine(Plugin.Instance.UI.SetupRoundEndDrops(Players.Select(k => k.GamePlayer).ToList(), roundEndCases, 1));
            }

            yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds);
            foreach (FFAPlayer player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.Game.SendPlayerToLobby(player.GamePlayer.Player, summaries.TryGetValue(player.GamePlayer, out MatchEndSummary pendingSummary) ? pendingSummary : null);
            }

            Players = new List<FFAPlayer>();

            List<int> locations = Plugin.Instance.Game.AvailableLocations;
            lock (locations)
            {
                string locString = "";
                foreach (int loc in locations)
                {
                    ArenaLocation locc = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == loc);
                    locString += $"{locc.LocationName},";
                }
                int randomLocation = locations.Count > 0 ? locations[UnityEngine.Random.Range(0, locations.Count)] : Location.LocationID;
                ArenaLocation location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == randomLocation);
                (EGameType, bool) gameMode = Plugin.Instance.Game.GetRandomGameMode(location.LocationID);
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
            player.OnGameJoined(this);

            FFAPlayer fPlayer = new(player);

            player.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            Players.Add(fPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, fPlayer);
            Plugin.Instance.UI.OnGameCountUpdated(this);

            Plugin.Instance.UI.SendLoadingUI(player.Player, true, GameMode, Location);
            for (int seconds = 1; seconds <= 5; seconds++)
            {
                yield return new WaitForSeconds(1);
                Plugin.Instance.UI.UpdateLoadingBar(player.Player, new string('　', Math.Min(96, seconds * 96 / 5)));
            }
            Vector3 currentPos = player.Player.Position;
            player.Player.Player.teleportToLocationUnsafe(new Vector3(currentPos.x, currentPos.y + 100, currentPos.z), 0);
            GiveLoadout(fPlayer);
            Plugin.Instance.UI.SendPreEndingUI(fPlayer.GamePlayer);

            switch (GamePhase)
            {
                case EGamePhase.WaitingForPlayers:
                    int minPlayers = Location.GetMinPlayers(GameMode);
                    if (Players.Count >= minPlayers)
                    {
                        GameStarter = Plugin.Instance.StartCoroutine(StartGame());
                    }
                    else
                    {
                        Plugin.Instance.UI.SendWaitingForPlayersUI(player, Players.Count, minPlayers);
                        foreach (FFAPlayer ply in Players)
                        {
                            if (ply == fPlayer)
                            {
                                continue;
                            }

                            Plugin.Instance.UI.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
                        }
                    }
                    SpawnPlayer(fPlayer);
                    break;
                case EGamePhase.Starting:
                    player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                    Plugin.Instance.UI.ShowCountdownUI(player);
                    SpawnPlayer(fPlayer);
                    break;
                case EGamePhase.Ending:
                    Plugin.Instance.UI.SetupFFALeaderboard(fPlayer, Players, Location, true, IsHardcore);
                    Plugin.Instance.UI.ShowFFALeaderboard(fPlayer.GamePlayer);
                    break;
                default:
                    Plugin.Instance.UI.SendFFAHUD(player);
                    Plugin.Instance.UI.UpdateFFATopUI(fPlayer, Players);
                    SpawnPlayer(fPlayer);
                    break;
            }

            Plugin.Instance.UI.ClearLoadingUI(player.Player);
            Plugin.Instance.UI.SendVoiceChatUI(player);
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            FFAPlayer fPlayer = GetFFAPlayer(player.Player);

            if (fPlayer == null)
            {
                return;
            }

            Plugin.Instance.UI.ClearPreEndingUI(player);
            Plugin.Instance.UI.ClearFFAHUD(player);
            Plugin.Instance.UI.ClearVoiceChatUI(player);
            Plugin.Instance.UI.ClearKillstreakUI(player);
            OnStoppedTalking(player);

            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UI.ClearCountdownUI(player);
                fPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }
            else if (GamePhase == EGamePhase.WaitingForPlayers)
            {
                Plugin.Instance.UI.ClearWaitingForPlayersUI(player);
            }

            player.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, 0);
            fPlayer.GamePlayer.OnGameLeft();
            Players.Remove(fPlayer);
            PlayersLookup.Remove(fPlayer.GamePlayer.SteamID);

            foreach (FFAPlayer ply in Players)
            {
                Plugin.Instance.UI.UpdateFFATopUI(ply, Players);
            }
            Plugin.Instance.UI.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            FFAPlayer fPlayer = GetFFAPlayer(player);
            if (fPlayer == null)
            {
                return;
            }

            if (cause == EDeathCause.SUICIDE)
            {
                RemovePlayerFromGame(fPlayer.GamePlayer);
                return;
            }

            if (fPlayer.GamePlayer.HasScoreboard)
            {
                fPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UI.HideFFALeaderboard(fPlayer.GamePlayer);
            }

            int victimKS = fPlayer.Killstreak;
            CSteamID updatedKiller = cause == EDeathCause.WATER ? fPlayer.GamePlayer.SteamID : (cause == EDeathCause.LANDMINE || cause == EDeathCause.SHRED ? (fPlayer.GamePlayer.LastDamager.Count > 0 ? fPlayer.GamePlayer.LastDamager.Pop() : killer) : killer);

            Logging.Debug($"Game player died, player name: {fPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");
            fPlayer.OnDeath(updatedKiller);
            fPlayer.GamePlayer.OnDeath(updatedKiller, Config.FFA.FileData.RespawnSeconds);

            Task.Run(async () => await Plugin.Instance.DB.IncreasePlayerDeathsAsync(fPlayer.GamePlayer.SteamID, 1));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                FFAPlayer kPlayer = GetFFAPlayer(updatedKiller);
                if (kPlayer == null)
                {
                    Logging.Debug("Killer not found, returning");
                    return;
                }

                if (kPlayer.GamePlayer.SteamID == fPlayer.GamePlayer.SteamID)
                {
                    OnKill(kPlayer.GamePlayer, fPlayer.GamePlayer, cause == EDeathCause.WATER ? (ushort)0 : (ushort)1, Config.FFA.FileData.KillFeedHexCode, Config.FFA.FileData.KillFeedHexCode);

                    Logging.Debug("Player killed themselves, returning");
                    return;
                }

                Dictionary<EQuestCondition, int> questConditions = new()
                {
                    { EQuestCondition.Map, Location.LocationID },
                    { EQuestCondition.Gamemode, (int)GameMode }
                };

                Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");

                if (fPlayer.GamePlayer.LastDamager.Count > 0 && fPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    fPlayer.GamePlayer.LastDamager.Pop();
                }

                if (fPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    FFAPlayer assister = GetFFAPlayer(fPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        assister.Assists++;
                        assister.Score += Config.Points.FileData.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UI.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP, Plugin.Instance.Translate("Assist_Kill", fPlayer.GamePlayer.Player.CharacterName.ToUnrich()));
                        }
                        Task.Run(async () => await Plugin.Instance.DB.IncreasePlayerXPAsync(assister.GamePlayer.SteamID, Config.Medals.FileData.AssistKillXP));
                    }
                    fPlayer.GamePlayer.LastDamager.Clear();
                }

                bool isFirstKill = Players[0].Kills == 0;
                kPlayer.Kills++;
                kPlayer.Score += Config.Points.FileData.KillPoints;

                int xpGained = 0;
                string xpText = "";
                ushort equipmentUsed = 0;
                float longshotRange = 0f;

                bool usedKillstreak = kPlayer.GamePlayer.HasKillstreakActive;
                int killstreakID = kPlayer.GamePlayer.ActiveKillstreak?.Killstreak?.KillstreakID ?? 0;

                if (usedKillstreak)
                {
                    Models.Data.KillstreakData info = kPlayer.GamePlayer.ActiveKillstreak.Killstreak.KillstreakInfo;
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
                            ushort equipment = kPlayer.GamePlayer.Player.Player.equipment.itemID;
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
                    string multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
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

                if (kPlayer.PlayersKilled.ContainsKey(fPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[fPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[fPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                    {
                        xpGained += Config.Medals.FileData.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                        Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Domination, questConditions);
                    }
                }
                else
                {
                    kPlayer.PlayersKilled.Add(fPlayer.GamePlayer.SteamID, 1);
                }

                if (fPlayer.GamePlayer.SteamID == kPlayer.GamePlayer.LastKiller)
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

                if (!usedKillstreak && cause == EDeathCause.GUN && (fPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
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
                kPlayer.GamePlayer.OnKilled(fPlayer.GamePlayer);

                if (equipmentUsed != 0)
                {
                    OnKill(kPlayer.GamePlayer, fPlayer.GamePlayer, equipmentUsed, Config.FFA.FileData.KillFeedHexCode, Config.FFA.FileData.KillFeedHexCode);
                }

                foreach (FFAPlayer ply in Players)
                {
                    Plugin.Instance.UI.UpdateFFATopUI(ply, Players);
                }
                if (kPlayer.Kills == Config.FFA.FileData.ScoreLimit)
                {
                    Plugin.Instance.StartCoroutine(GameEnd());
                }

                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Kill, questConditions);
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.MultiKill, questConditions);
                Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Killstreak, questConditions);
                if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                {
                    Plugin.Instance.Quest.CheckQuest(kPlayer.GamePlayer, EQuestType.Headshots, questConditions);
                }
                Plugin.Instance.Quest.CheckQuest(fPlayer.GamePlayer, EQuestType.Death, questConditions);

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
            FFAPlayer player = GetFFAPlayer(parameters.player);
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

            string damageReducePerkName = "none";
            string damageIncreasePerkName = "none";
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

            float damageReducePercent = player.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageReducePerkName, out LoadoutPerk damageReducerPerk) ? ((float)damageReducerPerk.Perk.SkillLevel / 100) : 0f;
            parameters.damage -= damageReducePercent * parameters.damage;

            player.GamePlayer.OnDamaged(parameters.killer);

            FFAPlayer kPlayer = GetFFAPlayer(parameters.killer);
            if (kPlayer == null)
            {
                return;
            }

            float damageIncreasePercent = kPlayer.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageIncreasePerkName, out LoadoutPerk damageIncreaserPerk) ? ((float)damageIncreaserPerk.Perk.SkillLevel / 100) : 0f;
            parameters.damage += damageIncreasePercent * parameters.damage;

            if (kPlayer.GamePlayer.HasSpawnProtection)
            {
                kPlayer.GamePlayer.SpawnProtectionRemover.Stop();
                kPlayer.GamePlayer.HasSpawnProtection = false;
            }
        }

        public override void OnPlayerRevived(UnturnedPlayer player)
        {
            FFAPlayer fPlayer = GetFFAPlayer(player);
            if (fPlayer == null)
            {
                return;
            }

            fPlayer.GamePlayer.OnRevived(Config.FFA.FileData.Kit, Config.FFA.FileData.TeamGloves);
        }

        public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition, ref float yaw)
        {
            if (!IsPlayerIngame(player.SteamID))
            {
                return;
            }

            FFASpawnPoint spawnPoint = GetFreeSpawn();
            respawnPosition = spawnPoint.GetSpawnPoint();
            yaw = spawnPoint.Yaw;
            player.GiveSpawnProtection(Config.FFA.FileData.SpawnProtectionSeconds);
        }

        public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
        {
            FFAPlayer fPlayer = GetFFAPlayer(player.Player);
            if (fPlayer == null)
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
                PlayerData data = player.Data;
                if (data.IsMuted)
                {
                    TimeSpan expiryTime = data.MuteExpiry.UtcDateTime - DateTime.UtcNow;
                    Utility.Say(player.Player, $"<color=red>You are muted for{(expiryTime.Days == 0 ? "" : $" {expiryTime.Days} Days ")}{(expiryTime.Hours == 0 ? "" : $" {expiryTime.Hours} Hours")} {expiryTime.Minutes} Minutes");
                    return;
                }

                string iconLink = Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "";
                string updatedText = $"[{Utility.ToFriendlyName(chatMode)}] <color={Utility.GetLevelColor(player.Data.Level)}>[{player.Data.Level}]</color> <color={Config.FFA.FileData.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={Config.FFA.FileData.ChatMessageHexCode}>{text.ToUnrich()}</color>";

                foreach (FFAPlayer reciever in Players)
                {
                    ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: player.Data.AvatarLink, useRichTextFormatting: true);
                }
            });
        }

        public override void OnVoiceChatUpdated(GamePlayer player)
        {
            SendVoiceChat(Players.Select(k => k.GamePlayer).ToList(), false);
        }

        public void GiveLoadout(FFAPlayer player)
        {
            player.GamePlayer.Player.Player.inventory.ClearInventory();
            Plugin.Instance.Loadout.GiveLoadout(player.GamePlayer, Config.FFA.FileData.Kit, Config.FFA.FileData.TeamGloves);
        }

        public void SpawnPlayer(FFAPlayer player)
        {
            if (SpawnPoints.Count == 0) return;

            FFASpawnPoint spawnPoint = GetFreeSpawn();
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), spawnPoint.Yaw);
            player.GamePlayer.GiveSpawnProtection(Config.FFA.FileData.SpawnProtectionSeconds);
        }

        public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
        {
            FFAPlayer fPlayer = GetFFAPlayer(player.Player);
            if (fPlayer == null)
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
            FFAPlayer fPlayer = GetFFAPlayer(player.Player);
            if (fPlayer == null)
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
            FFAPlayer fPlayer = GetFFAPlayer(player.Player);
            if (fPlayer == null)
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

            if (fPlayer.GamePlayer.HasScoreboard)
            {
                fPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UI.HideFFALeaderboard(fPlayer.GamePlayer);
            }
            else
            {
                fPlayer.GamePlayer.HasScoreboard = true;
                Plugin.Instance.UI.SetupFFALeaderboard(fPlayer, Players, Location, true, IsHardcore);
                Plugin.Instance.UI.ShowFFALeaderboard(fPlayer.GamePlayer);
            }
        }

        public override void PlayerStanceChanged(PlayerStance obj)
        {
            FFAPlayer fPlayer = GetFFAPlayer(obj.player);
            if (fPlayer == null)
            {
                return;
            }

            fPlayer.GamePlayer.OnStanceChanged(obj.stance);
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

        public IEnumerator SpawnUsedUp(FFASpawnPoint spawnPoint)
        {
            SpawnPoints.Remove(spawnPoint);
            UnavailableSpawnPoints.Add(spawnPoint);
            yield return new WaitForSeconds(Config.Base.FileData.SpawnUnavailableSeconds);
            SpawnPoints.Add(spawnPoint);
            UnavailableSpawnPoints.Remove(spawnPoint);
        }

        public FFAPlayer GetFFAPlayer(CSteamID steamID)
        {
            return PlayersLookup.TryGetValue(steamID, out FFAPlayer fPlayer) ? fPlayer : null;
        }

        public FFAPlayer GetFFAPlayer(UnturnedPlayer player)
        {
            return PlayersLookup.TryGetValue(player.CSteamID, out FFAPlayer fPlayer) ? fPlayer : null;
        }

        public FFAPlayer GetFFAPlayer(Player player)
        {
            return PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out FFAPlayer fPlayer) ? fPlayer : null;
        }

        public FFASpawnPoint GetFreeSpawn()
        {
            return SpawnPoints.Where(k => !IsPlayerNearPosition(k.GetSpawnPoint(), Location.PositionCheck)).ToList().RandomOrDefault() ?? SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)];
        }

        public bool IsPlayerNearPosition(Vector3 position, float radius)
        {
            return Players.Exists(k => (k.GamePlayer.Player.Position - position).sqrMagnitude < radius);
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

        public override void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow)
        {

        }

        public override List<GamePlayer> GetPlayers()
        {
            return Players.Select(k => k.GamePlayer).ToList();
        }

        public override bool IsPlayerCarryingFlag(GamePlayer player)
        {
            return false;
        }
    }
}
