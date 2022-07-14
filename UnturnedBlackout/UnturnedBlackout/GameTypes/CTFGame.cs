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
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.GameTypes
{
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
            SpawnPoints = new Dictionary<int, List<CTFSpawnPoint>>();
            var blueFlag = Vector3.zero;
            var redFlag = Vector3.zero;

            foreach (var spawnPoint in Plugin.Instance.DataManager.Data.CTFSpawnPoints.Where(k => k.LocationID == location.LocationID))
            {
                if (spawnPoint.IsFlagSP)
                {
                    if ((ETeam)spawnPoint.GroupID == ETeam.Blue)
                    {
                        blueFlag = spawnPoint.GetSpawnPoint();
                    }
                    else if ((ETeam)spawnPoint.GroupID == ETeam.Red)
                    {
                        redFlag = spawnPoint.GetSpawnPoint();
                    }
                    continue;
                }

                if (SpawnPoints.TryGetValue(spawnPoint.GroupID, out List<CTFSpawnPoint> spawnPoints))
                {
                    spawnPoints.Add(spawnPoint);
                }
                else
                {
                    SpawnPoints.Add(spawnPoint.GroupID, new List<CTFSpawnPoint> { spawnPoint });
                }
            }

            Players = new List<CTFPlayer>();
            PlayersLookup = new Dictionary<CSteamID, CTFPlayer>();

            var blueTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
            var redTeamInfo = Config.Teams.FileData.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

            BlueTeam = new CTFTeam((byte)ETeam.Blue, false, blueTeamInfo, Config.CTF.FileData.BlueFlagID, blueFlag);
            RedTeam = new CTFTeam((byte)ETeam.Red, false, redTeamInfo, Config.CTF.FileData.RedFlagID, redFlag);
            Frequency = Utility.GetFreeFrequency();
        }

        public IEnumerator StartGame()
        {
            GamePhase = EGamePhase.Starting;
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ClearWaitingForPlayersUI(player.GamePlayer);
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UIManager.ShowCountdownUI(player.GamePlayer);
                SpawnPlayer(player);
            }

            for (int seconds = Config.CTF.FileData.StartSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.SendCountdownSeconds(player.GamePlayer, seconds);
                }
            }
            GamePhase = EGamePhase.Started;
            foreach (var player in Players)
            {
                player.GamePlayer.GiveMovement(player.GamePlayer.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, false);

                Plugin.Instance.UIManager.ClearCountdownUI(player.GamePlayer);
            }
            Plugin.Instance.UIManager.SendCTFHUD(BlueTeam, RedTeam, Players);

            TaskDispatcher.QueueOnMainThread(() =>
            {
                WipeItems();
                ItemManager.dropItem(new Item(RedTeam.FlagID, true), RedTeam.FlagSP, true, true, true);
                ItemManager.dropItem(new Item(BlueTeam.FlagID, true), BlueTeam.FlagSP, true, true, true);
                Logging.Debug($"Dropping red flag at {RedTeam.FlagSP} and blue flag at {BlueTeam.FlagSP} for location {Location.LocationName}");
            });

            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.CTF.FileData.EndSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.UpdateCTFTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
                }
            }

            CTFTeam wonTeam;
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
                wonTeam = new CTFTeam(-1, true, new TeamInfo(), 0, Vector3.zero);
            }
            Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
        }

        public IEnumerator GameEnd(CTFTeam wonTeam)
        {
            if (GameEnder != null)
            {
                Plugin.Instance.StopCoroutine(GameEnder);
            }

            GamePhase = EGamePhase.Ending;
            Plugin.Instance.UIManager.OnGameUpdated();

            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ClearCTFHUD(player.GamePlayer);
                Plugin.Instance.UIManager.ClearMidgameLoadoutUI(player.GamePlayer);

                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UIManager.HideCTFLeaderboard(player.GamePlayer);
                }
                if (player.Team == wonTeam)
                {
                    var xp = player.XP * Config.CTF.FileData.WinMultiplier;
                    TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.QuestManager.CheckQuest(player.GamePlayer, EQuestType.Win, new Dictionary<EQuestCondition, int> { { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode }, { EQuestCondition.WinFlagsCaptured, player.FlagsCaptured }, { EQuestCondition.WinFlagsSaved, player.FlagsSaved }, { EQuestCondition.WinKills, player.Kills } }));
                    ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(player.GamePlayer.SteamID, (int)xp));
                }
                Plugin.Instance.UIManager.SetupPreEndingUI(player.GamePlayer, EGameType.CTF, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName);
                player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UIManager.SetupCTFLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false, IsHardcore);
                WipeItems();
            });
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ShowCTFLeaderboard(player.GamePlayer);
            }
            yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds);
            foreach (var player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.GameManager.SendPlayerToLobby(player.GamePlayer.Player);
            }

            Players = new List<CTFPlayer>();
            BlueTeam.Destroy();
            RedTeam.Destroy();

            var locations = Plugin.Instance.GameManager.AvailableLocations;
            lock (locations)
            {
                var randomLocation = locations.Count > 0 ? locations[UnityEngine.Random.Range(0, locations.Count)] : Location.LocationID;
                var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == randomLocation);
                var gameMode = Plugin.Instance.GameManager.GetRandomGameMode(location.LocationID);
                GamePhase = EGamePhase.Ended;
                Plugin.Instance.GameManager.EndGame(this);
                Plugin.Instance.GameManager.StartGame(location, gameMode.Item1, gameMode.Item2);
            }
        }

        public override IEnumerator AddPlayerToGame(GamePlayer player)
        {
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                yield break;
            }

            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;
            CTFPlayer cPlayer = new(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(cPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, cPlayer);

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
            Plugin.Instance.UIManager.SendLoadingUI(player.Player, true, GameMode, Location);
            for (int seconds = 1; seconds <= 5; seconds++)
            {
                yield return new WaitForSeconds(1);
                Plugin.Instance.UIManager.UpdateLoadingBar(player.Player, new string('　', Math.Min(96, seconds * 96 / 5)));
            }
            var currentPos = player.Player.Position;
            player.Player.Player.teleportToLocationUnsafe(new Vector3(currentPos.x, currentPos.y + 100, currentPos.z), 0);
            GiveLoadout(cPlayer);
            Plugin.Instance.UIManager.SendPreEndingUI(cPlayer.GamePlayer);
            SpawnPlayer(cPlayer);
            Plugin.Instance.UIManager.ClearLoadingUI(player.Player);
            Plugin.Instance.UIManager.SendVoiceChatUI(player);

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
                        Plugin.Instance.UIManager.SendWaitingForPlayersUI(player, Players.Count, minPlayers);
                        foreach (var ply in Players)
                        {
                            if (ply == cPlayer)
                            {
                                continue;
                            }

                            Plugin.Instance.UIManager.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
                        }
                    }
                    break;
                case EGamePhase.Starting:
                    player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                    Plugin.Instance.UIManager.ShowCountdownUI(player);
                    break;
                case EGamePhase.Ending:
                    CTFTeam wonTeam;
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
                        wonTeam = new CTFTeam(-1, true, new TeamInfo(), 0, Vector3.zero);
                    }
                    Plugin.Instance.UIManager.SetupCTFLeaderboard(cPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
                    Plugin.Instance.UIManager.ShowCTFLeaderboard(cPlayer.GamePlayer);
                    break;
                default:
                    Plugin.Instance.UIManager.SendCTFHUD(cPlayer, BlueTeam, RedTeam, Players);
                    break;
            }
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                return;
            }

            var cPlayer = GetCTFPlayer(player.Player);

            Plugin.Instance.UIManager.ClearCTFHUD(player);
            Plugin.Instance.UIManager.ClearPreEndingUI(player);
            Plugin.Instance.UIManager.ClearVoiceChatUI(player);

            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UIManager.ClearCountdownUI(player);
                cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }
            else if (GamePhase == EGamePhase.WaitingForPlayers)
            {
                Plugin.Instance.UIManager.ClearWaitingForPlayersUI(player);
            }

            if (cPlayer != null)
            {
                cPlayer.Team.RemovePlayer(cPlayer.GamePlayer.SteamID);
                cPlayer.GamePlayer.OnGameLeft();
                Players.Remove(cPlayer);
                PlayersLookup.Remove(cPlayer.GamePlayer.SteamID);

                if (cPlayer.IsCarryingFlag)
                {
                    var otherTeam = cPlayer.Team.TeamID == BlueTeam.TeamID ? RedTeam : BlueTeam;
                    if (cPlayer.GamePlayer.Player.Player.clothing.backpack == otherTeam.FlagID)
                    {
                        ItemManager.dropItem(new Item(otherTeam.FlagID, true), cPlayer.GamePlayer.Player.Player.transform.position, true, true, true);
                    }
                    cPlayer.IsCarryingFlag = false;
                    cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1f);
                    TaskDispatcher.QueueOnMainThread(() =>
                    {
                        Plugin.Instance.UIManager.UpdateCTFHUD(Players, otherTeam);
                        Plugin.Instance.UIManager.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.Dropped);
                    });
                }
            }

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            var cPlayer = GetCTFPlayer(player);
            if (cPlayer == null)
            {
                return;
            }
            if (cPlayer.GamePlayer.HasScoreboard)
            {
                cPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideCTFLeaderboard(cPlayer.GamePlayer);
            }

            var victimKS = cPlayer.KillStreak;
            Logging.Debug($"Game player died, player name: {cPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");
            
            var updatedKiller = cause == EDeathCause.WATER ? cPlayer.GamePlayer.SteamID : (cause == EDeathCause.LANDMINE || cause == EDeathCause.SHRED ? (cPlayer.GamePlayer.LastDamager.Count > 0 ? cPlayer.GamePlayer.LastDamager.Pop() : killer) : killer);

            cPlayer.OnDeath(updatedKiller);
            cPlayer.GamePlayer.OnDeath(updatedKiller, Config.CTF.FileData.RespawnSeconds);

            var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;
            var isFlagCarrier = false;

            if (cPlayer.IsCarryingFlag)
            {
                isFlagCarrier = true;
                if (player.clothing.backpack == otherTeam.FlagID)
                {
                    ItemManager.dropItem(new Item(otherTeam.FlagID, true), cause == EDeathCause.WATER ? otherTeam.FlagSP : player.transform.position, true, true, true);
                }
                cPlayer.IsCarryingFlag = false;
                cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1f);
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    Plugin.Instance.UIManager.UpdateCTFHUD(Players, otherTeam);
                    Plugin.Instance.UIManager.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.Dropped);
                });
            }

            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(cPlayer.GamePlayer.SteamID, 1));

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
                    Logging.Debug("Player killed themselves, returning");
                    return;
                }

                var questConditions = new Dictionary<EQuestCondition, int>
                {
                    { EQuestCondition.Map, Location.LocationID },
                    { EQuestCondition.Gamemode, (int)GameMode }
                };

                Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");

                if (cPlayer.GamePlayer.LastDamager.Count > 0 && cPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    cPlayer.GamePlayer.LastDamager.Pop();
                }

                if (cPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    var assister = GetCTFPlayer(cPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        assister.Assists++;
                        assister.Score += Config.Points.FileData.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UIManager.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP, Plugin.Instance.Translate("Assist_Kill", cPlayer.GamePlayer.Player.CharacterName.ToUnrich()));
                        }
                        ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(assister.GamePlayer.SteamID,
                            Config.Medals.FileData.AssistKillXP));
                    }
                    cPlayer.GamePlayer.LastDamager.Clear();
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

                kPlayer.KillStreak++;
                questConditions.Add(EQuestCondition.TargetKS, kPlayer.KillStreak);
                if (kPlayer.MultipleKills == 0)
                {
                    kPlayer.MultipleKills++;
                }
                else if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
                {
                    xpGained += Config.Medals.FileData.BaseXPMK + (++kPlayer.MultipleKills * Config.Medals.FileData.IncreaseXPPerMK);
                    var multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
                    xpText += (multiKillText == $"Multiple_Kills_Show_{kPlayer.MultipleKills}" ? Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich() : multiKillText) + "\n";
                }
                else
                {
                    kPlayer.MultipleKills = 1;
                }
                questConditions.Add(EQuestCondition.TargetMK, kPlayer.MultipleKills);

                if (victimKS > Config.Medals.FileData.ShutdownKillStreak)
                {
                    xpGained += Config.Medals.FileData.ShutdownXP;
                    xpText += Plugin.Instance.Translate("Shutdown_Kill").ToRich() + "\n";
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Shutdown, questConditions);
                }

                if (kPlayer.PlayersKilled.ContainsKey(cPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[cPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[cPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                    {
                        xpGained += Config.Medals.FileData.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                        Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Domination, questConditions);
                    }
                }
                else
                {
                    kPlayer.PlayersKilled.Add(cPlayer.GamePlayer.SteamID, 1);
                }

                if (isFlagCarrier)
                {
                    xpGained += Config.Medals.FileData.FlagCarrierKilledXP;
                    xpText += Plugin.Instance.Translate("Flag_Killer").ToRich() + "\n";
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.FlagKiller, questConditions);
                }

                if (kPlayer.IsCarryingFlag)
                {
                    xpGained += Config.Medals.FileData.KillWhileCarryingFlagXP;
                    xpText += Plugin.Instance.Translate("Flag_Denied").ToRich() + "\n";
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.FlagDenied, questConditions);
                }

                if (cPlayer.GamePlayer.SteamID == kPlayer.GamePlayer.LastKiller)
                {
                    xpGained += Config.Medals.FileData.RevengeXP;
                    xpText += Plugin.Instance.Translate("Revenge_Kill").ToRich() + "\n";
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Revenge, questConditions);
                }

                if (isFirstKill)
                {
                    xpGained += Config.Medals.FileData.FirstKillXP;
                    xpText += Plugin.Instance.Translate("First_Kill").ToRich() + "\n";
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.FirstKill, questConditions);
                }

                if (cause == EDeathCause.GUN && (cPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
                {
                    xpGained += Config.Medals.FileData.LongshotXP;
                    xpText += Plugin.Instance.Translate("Longshot_Kill").ToRich() + "\n";
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Longshot, questConditions);
                }

                if (kPlayer.GamePlayer.Player.Player.life.health < Config.Medals.FileData.HealthSurvivorKill)
                {
                    xpGained += Config.Medals.FileData.SurvivorXP;
                    xpText += Plugin.Instance.Translate("Survivor_Kill").ToRich() + "\n";
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Survivor, questConditions);
                }

                kPlayer.GamePlayer.LastKiller = CSteamID.Nil;
                kPlayer.LastKill = DateTime.UtcNow;
                kPlayer.XP += xpGained;
                Players.Sort((x, y) => y.Kills.CompareTo(x.Kills));

                Plugin.Instance.UIManager.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);
                Plugin.Instance.UIManager.SendMultiKillSound(kPlayer.GamePlayer, kPlayer.MultipleKills);
                kPlayer.CheckKills();
                kPlayer.GamePlayer.OnKilled(cPlayer.GamePlayer);

                if (equipmentUsed != 0)
                {
                    Logging.Debug($"Sending killfeed with equipment {equipmentUsed}");
                    OnKill(kPlayer.GamePlayer, cPlayer.GamePlayer, equipmentUsed, kPlayer.Team.Info.KillFeedHexCode, cPlayer.Team.Info.KillFeedHexCode);
                }

                Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Kill, questConditions);
                Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.MultiKill, questConditions);
                Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Killstreak, questConditions);
                if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                {
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Headshots, questConditions);
                }
                Plugin.Instance.QuestManager.CheckQuest(cPlayer.GamePlayer, EQuestType.Death, questConditions);

                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    if (cause == EDeathCause.GUN && limb == ELimb.SKULL)
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerHeadshotKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    else
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, xpGained);
                    if ((kPlayer.GamePlayer.ActiveLoadout.Primary != null && kPlayer.GamePlayer.ActiveLoadout.Primary.Gun.GunID == equipmentUsed) || (kPlayer.GamePlayer.ActiveLoadout.Secondary != null && kPlayer.GamePlayer.ActiveLoadout.Secondary.Gun.GunID == equipmentUsed))
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerGunXPAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, xpGained);
                        await Plugin.Instance.DBManager.IncreasePlayerGunKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                    else if (kPlayer.GamePlayer.ActiveLoadout.Lethal != null && kPlayer.GamePlayer.ActiveLoadout.Lethal.Gadget.GadgetID == equipmentUsed)
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerGadgetKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                    else if (kPlayer.GamePlayer.ActiveLoadout.Killstreaks.Select(k => k.Killstreak.KillstreakID).Contains(equipmentUsed))
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerKillstreakKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                    else if (kPlayer.GamePlayer.ActiveLoadout.Knife != null && kPlayer.GamePlayer.ActiveLoadout.Knife.Knife.KnifeID == equipmentUsed)
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerKnifeKillsAsync(kPlayer.GamePlayer.SteamID, equipmentUsed, 1);
                    }
                });
            });
        }

        public override void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            var player = GetCTFPlayer(parameters.player);
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

            var kPlayer = GetCTFPlayer(parameters.killer);
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
                kPlayer.GamePlayer.m_RemoveSpawnProtection.Stop();
                kPlayer.GamePlayer.HasSpawnProtection = false;
            }
        }

        public override void OnPlayerRevived(UnturnedPlayer player)
        {
            var cPlayer = GetCTFPlayer(player);
            if (cPlayer == null)
            {
                return;
            }

            var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;
            if (otherTeam.FlagID == player.Player.clothing.backpack)
            {
                player.Player.clothing.thirdClothes.backpack = 0;
                player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);
            }

            cPlayer.GamePlayer.OnRevived(cPlayer.Team.Info.TeamKits[UnityEngine.Random.Range(0, cPlayer.Team.Info.TeamKits.Count)], cPlayer.Team.Info.TeamGloves);
        }

        public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition, ref float yaw)
        {
            var cPlayer = GetCTFPlayer(player.Player);
            if (cPlayer == null)
            {
                return;
            }

            if (!SpawnPoints.TryGetValue(cPlayer.Team.SpawnPoint, out var spawnPoints))
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
            player.GiveSpawnProtection(Config.CTF.FileData.SpawnProtectionSeconds);
        }

        public override void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow)
        {
            var cPlayer = GetCTFPlayer(player.Player);
            if (cPlayer == null)
            {
                return;
            }

            Logging.Debug($"{player.Player.CharacterName} is trying to pick up item {itemData.item.id}");
            var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;

            if (player.Player.Player.equipment.isBusy)
            {
                shouldAllow = false;
                return;
            }

            var questConditions = new Dictionary<EQuestCondition, int>
            {
                { EQuestCondition.Map, Location.LocationID },
                { EQuestCondition.Gamemode, (int)GameMode }
            };

            if (cPlayer.Team.FlagID == itemData.item.id)
            {
                Logging.Debug($"{player.Player.CharacterName} is trying to pick up their own flag, checking if they are saving the flag");
                shouldAllow = false;

                if (!cPlayer.Team.HasFlag)
                {
                    Logging.Debug($"{player.Player.CharacterName} is saving their flag, clearing the flag and putting it back into position");
                    Logging.Debug($"Spawning their team's flag at {cPlayer.Team.FlagSP} for location {Location.LocationName}");
                    ItemManager.ServerClearItemsInSphere(itemData.point, 1);
                    ItemManager.dropItem(new Item(cPlayer.Team.FlagID, true), cPlayer.Team.FlagSP, true, true, true);
                    cPlayer.Team.HasFlag = true;
                    cPlayer.Score += Config.Points.FileData.FlagSavedPoints;
                    cPlayer.XP += Config.Medals.FileData.FlagSavedXP;
                    cPlayer.FlagsSaved++;
                    Plugin.Instance.UIManager.ShowXPUI(cPlayer.GamePlayer, Config.Medals.FileData.FlagSavedXP, Plugin.Instance.Translate("Flag_Saved").ToRich());
                    Plugin.Instance.UIManager.SendFlagSavedSound(cPlayer.GamePlayer);

                    TaskDispatcher.QueueOnMainThread(() =>
                    {
                        Plugin.Instance.UIManager.UpdateCTFHUD(Players, cPlayer.Team);
                        Plugin.Instance.UIManager.SendCTFFlagStates(cPlayer.Team, (ETeam)cPlayer.Team.TeamID, Players, EFlagState.Recovered);
                        Plugin.Instance.QuestManager.CheckQuest(player, EQuestType.FlagsSaved, questConditions);
                    });

                    ThreadPool.QueueUserWorkItem(async (o) =>
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerFlagsSavedAsync(cPlayer.GamePlayer.SteamID, 1);
                        await Plugin.Instance.DBManager.IncreasePlayerXPAsync(cPlayer.GamePlayer.SteamID, Config.Medals.FileData.FlagSavedXP);
                    });
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

                    ItemManager.dropItem(new Item(otherTeam.FlagID, true), otherTeam.FlagSP, true, true, true);
                    Logging.Debug($"Spawning the other team's flag at {otherTeam.FlagSP} for location {Location.LocationName}");
                    otherTeam.HasFlag = true;
                    cPlayer.Team.Score++;
                    cPlayer.Score += Config.Points.FileData.FlagCapturedPoints;
                    cPlayer.XP += Config.Medals.FileData.FlagCapturedXP;
                    cPlayer.FlagsCaptured++;
                    player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, false);

                    Plugin.Instance.UIManager.ShowXPUI(cPlayer.GamePlayer, Config.Medals.FileData.FlagCapturedXP, Plugin.Instance.Translate("Flag_Captured").ToRich());
                    Plugin.Instance.UIManager.SendFlagCapturedSound(cPlayer.GamePlayer);

                    TaskDispatcher.QueueOnMainThread(() =>
                    {
                        Plugin.Instance.UIManager.UpdateCTFHUD(Players, cPlayer.Team);
                        Plugin.Instance.UIManager.UpdateCTFHUD(Players, otherTeam);
                        Plugin.Instance.UIManager.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.Captured);
                        Plugin.Instance.QuestManager.CheckQuest(player, EQuestType.FlagsCaptured, questConditions);
                    });

                    if (cPlayer.Team.Score >= Config.CTF.FileData.ScoreLimit)
                    {
                        Plugin.Instance.StartCoroutine(GameEnd(cPlayer.Team));
                    }

                    ThreadPool.QueueUserWorkItem(async (o) =>
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerFlagsCapturedAsync(cPlayer.GamePlayer.SteamID, 1);
                        await Plugin.Instance.DBManager.IncreasePlayerXPAsync(cPlayer.GamePlayer.SteamID, Config.Medals.FileData.FlagCapturedXP);
                    });
                }
                else
                {
                    Logging.Debug($"[ERROR] Could'nt find the other team's flag as the player's backpack");
                }

                cPlayer.IsCarryingFlag = false;
            }
            else if (otherTeam.FlagID == itemData.item.id)
            {
                Logging.Debug($"{player.Player.CharacterName} is trying to pick up the other team's flag, checking if they have their own flag");
                /*if (!cPlayer.Team.HasFlag)
                {
                    Logging.Debug($"Their own team doesn't have their flag, don't allow to pickup flag");
                    shouldAllow = false;
                    return;
                }*/

                if (otherTeam.HasFlag)
                {
                    TaskDispatcher.QueueOnMainThread(() =>
                    {
                        Plugin.Instance.UIManager.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.Taken);
                    });
                    otherTeam.HasFlag = false;
                }
                else
                {
                    TaskDispatcher.QueueOnMainThread(() =>
                    {
                        Plugin.Instance.UIManager.SendCTFFlagStates(cPlayer.Team, (ETeam)otherTeam.TeamID, Players, EFlagState.Picked);
                    });
                }

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    var ply = cPlayer.GamePlayer.Player.Player;
                    if (ply.equipment.equippedPage == 0)
                    {
                        var secondary = ply.inventory.getItem(1, 0);
                        if (secondary != null)
                        {
                            ply.equipment.tryEquip(1, secondary.x, secondary.y);
                        }
                        else
                        {
                            ply.equipment.dequip();
                        }
                    }
                });

                player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, true, false);
                cPlayer.IsCarryingFlag = true;
                Plugin.Instance.UIManager.UpdateCTFHUD(Players, otherTeam);
            }
        }

        public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
        {
            var tPlayer = GetCTFPlayer(player.Player);
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

                var iconLink = Plugin.Instance.DBManager.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "";
                var updatedText = $"<color={tPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={tPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

                if (chatMode == EChatMode.GLOBAL)
                {
                    foreach (var reciever in Players)
                    {
                        ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: iconLink, useRichTextFormatting: true);
                    }
                    return;
                }

                var teamPlayers = Players.Where(k => k.Team == tPlayer.Team);
                foreach (var reciever in teamPlayers)
                {
                    ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: iconLink, useRichTextFormatting: true);
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

            var tPlayer = GetCTFPlayer(player.Player);
            SendVoiceChat(Players.Where(k => k.Team == tPlayer.Team).Select(k => k.GamePlayer).ToList(), true);
        }

        public void GiveLoadout(CTFPlayer player)
        {
            player.GamePlayer.Player.Player.inventory.ClearInventory();
            Plugin.Instance.LoadoutManager.GiveLoadout(player.GamePlayer, player.Team.Info.TeamKits[UnityEngine.Random.Range(0, player.Team.Info.TeamKits.Count)], player.Team.Info.TeamGloves);
        }

        public void SpawnPlayer(CTFPlayer player)
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
            player.GamePlayer.GiveSpawnProtection(Config.CTF.FileData.SpawnProtectionSeconds);
        }

        public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
        {
            var cPlayer = GetCTFPlayer(player.Player);
            if (cPlayer == null)
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
            var cPlayer = GetCTFPlayer(player.Player);
            if (cPlayer == null)
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
            var cPlayer = GetCTFPlayer(player.Player);
            if (cPlayer == null)
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
            player.ScoreboardCooldown = DateTime.UtcNow.AddSeconds(1);

            if (cPlayer.GamePlayer.HasScoreboard)
            {
                cPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideCTFLeaderboard(cPlayer.GamePlayer);
            }
            else
            {
                cPlayer.GamePlayer.HasScoreboard = true;
                CTFTeam wonTeam;
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
                    wonTeam = new CTFTeam(-1, true, new TeamInfo(), 0, Vector3.zero);
                }

                Plugin.Instance.UIManager.SetupCTFLeaderboard(cPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true, IsHardcore);
                Plugin.Instance.UIManager.ShowCTFLeaderboard(cPlayer.GamePlayer);
            }
        }

        public override void PlayerStanceChanged(PlayerStance obj)
        {
            var cPlayer = GetCTFPlayer(obj.player);
            if (cPlayer == null)
            {
                return;
            }
            cPlayer.GamePlayer.OnStanceChanged(obj.stance);
        }

        public override void PlayerEquipmentChanged(GamePlayer player)
        {
            var cPlayer = GetCTFPlayer(player.Player);
            if (cPlayer == null)
            {
                return;
            }

            if (GamePhase == EGamePhase.Starting)
            {
                return;
            }

            player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, cPlayer.IsCarryingFlag, true);
        }

        public override void PlayerAimingChanged(GamePlayer player, bool isAiming)
        {
            var cPlayer = GetCTFPlayer(player.Player);
            if (cPlayer == null)
            {
                return;
            }

            if (GamePhase == EGamePhase.Starting)
            {
                return;
            }

            player.GiveMovement(isAiming, cPlayer.IsCarryingFlag, false);
        }

        public CTFPlayer GetCTFPlayer(CSteamID steamID)
        {
            return PlayersLookup.TryGetValue(steamID, out CTFPlayer cPlayer) ? cPlayer : null;
        }

        public CTFPlayer GetCTFPlayer(UnturnedPlayer player)
        {
            return PlayersLookup.TryGetValue(player.CSteamID, out CTFPlayer cPlayer) ? cPlayer : null;
        }

        public CTFPlayer GetCTFPlayer(Player player)
        {
            return PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out CTFPlayer cPlayer) ? cPlayer : null;
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

        public override bool IsPlayerCarryingFlag(GamePlayer player)
        {
            var cPlayer = GetCTFPlayer(player.SteamID);
            if (cPlayer != null)
            {
                return cPlayer.IsCarryingFlag;
            }
            return false;
        }
    }
}
