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
            SpawnPoints = Plugin.Instance.DataManager.Data.FFASpawnPoints.Where(k => k.LocationID == location.LocationID).ToList();
            Players = new List<FFAPlayer>();
            PlayersLookup = new Dictionary<CSteamID, FFAPlayer>();
            UnavailableSpawnPoints = new List<FFASpawnPoint>();
            Frequency = Utility.GetFreeFrequency();
        }

        public IEnumerator StartGame()
        {
            TaskDispatcher.QueueOnMainThread(() => WipeItems());
            GamePhase = EGamePhase.Starting;
            Plugin.Instance.UIManager.OnGameUpdated();
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ClearWaitingForPlayersUI(player.GamePlayer);
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UIManager.ShowCountdownUI(player.GamePlayer);
                SpawnPlayer(player, true);
            }

            for (int seconds = Config.FFA.FileData.StartSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.SendCountdownSeconds(player.GamePlayer, seconds);
                }
            }
            GamePhase = EGamePhase.Started;
            Plugin.Instance.UIManager.OnGameUpdated();
            foreach (var player in Players)
            {
                player.GamePlayer.GiveMovement(player.GamePlayer.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, false);

                Plugin.Instance.UIManager.SendFFAHUD(player.GamePlayer);
                Plugin.Instance.UIManager.ClearCountdownUI(player.GamePlayer);
                Plugin.Instance.UIManager.UpdateFFATopUI(player, Players);
            }

            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.FFA.FileData.EndSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.UpdateFFATimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
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
            Plugin.Instance.UIManager.OnGameUpdated();
            for (int index = 0; index < Players.Count; index++)
            {
                var player = Players[index];
                Plugin.Instance.UIManager.ClearFFAHUD(player.GamePlayer);
                Plugin.Instance.UIManager.ClearMidgameLoadoutUI(player.GamePlayer);
                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UIManager.HideFFALeaderboard(player.GamePlayer);
                }
                Plugin.Instance.UIManager.SetupPreEndingUI(player.GamePlayer, EGameType.FFA, index == 0, 0, 0, "", "");
                if (index == 0)
                {
                    var xp = player.XP * Config.FFA.FileData.WinMultiplier;
                    TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.QuestManager.CheckQuest(player.GamePlayer, EQuestType.Win, new Dictionary<EQuestCondition, int> { { EQuestCondition.Map, Location.LocationID }, { EQuestCondition.Gamemode, (int)GameMode }, { EQuestCondition.WinKills, player.Kills } }));
                    ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(player.GamePlayer.SteamID, (int)xp));
                }
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UIManager.SetupFFALeaderboard(Players, Location, false, IsHardcore);
                WipeItems();
            });
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ShowFFALeaderboard(player.GamePlayer);
            }
            yield return new WaitForSeconds(Config.Base.FileData.EndingLeaderboardSeconds);
            foreach (var player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.GameManager.SendPlayerToLobby(player.GamePlayer.Player);
            }

            Players = new List<FFAPlayer>();

            var locations = Plugin.Instance.GameManager.AvailableLocations;
            lock (locations)
            {
                locations.Add(Location.LocationID);
                var randomLocation = locations[UnityEngine.Random.Range(0, locations.Count)];
                var location = Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == randomLocation);
                var gameMode = Plugin.Instance.GameManager.GetRandomGameMode(location?.LocationID ?? Location.LocationID);
                GamePhase = EGamePhase.Ended;
                Plugin.Instance.GameManager.EndGame(this);
                Plugin.Instance.GameManager.StartGame(location ?? Location, gameMode.Item1, gameMode.Item2);
            }
        }

        public override IEnumerator AddPlayerToGame(GamePlayer player)
        {
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                yield break;
            }

            FFAPlayer fPlayer = new(player);

            player.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            Players.Add(fPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, fPlayer);
            Plugin.Instance.UIManager.OnGameCountUpdated(this);

            Plugin.Instance.UIManager.SendLoadingUI(player.Player, true, GameMode, Location);
            for (int seconds = 1; seconds <= 5; seconds++)
            {
                yield return new WaitForSeconds(1);
                Plugin.Instance.UIManager.UpdateLoadingBar(player.Player, new string('　', Math.Min(96, seconds * 96 / 5)));
            }
            var currentPos = player.Player.Position;
            player.Player.Player.teleportToLocationUnsafe(new Vector3(currentPos.x, currentPos.y + 100, currentPos.z), 0);
            GiveLoadout(fPlayer);
            Plugin.Instance.UIManager.SendPreEndingUI(fPlayer.GamePlayer);

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
                            if (ply == fPlayer)
                            {
                                continue;
                            }

                            Plugin.Instance.UIManager.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
                        }
                    }
                    SpawnPlayer(fPlayer, true);
                    break;
                case EGamePhase.Starting:
                    player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                    Plugin.Instance.UIManager.ShowCountdownUI(player);
                    SpawnPlayer(fPlayer, true);
                    break;
                case EGamePhase.Ending:
                    Plugin.Instance.UIManager.SetupFFALeaderboard(fPlayer, Players, Location, true, IsHardcore);
                    Plugin.Instance.UIManager.ShowFFALeaderboard(fPlayer.GamePlayer);
                    break;
                default:
                    Plugin.Instance.UIManager.SendFFAHUD(player);
                    Plugin.Instance.UIManager.UpdateFFATopUI(fPlayer, Players);
                    SpawnPlayer(fPlayer, false);
                    break;
            }

            Plugin.Instance.UIManager.ClearLoadingUI(player.Player);
            Plugin.Instance.UIManager.SendVoiceChatUI(player);
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            var fPlayer = GetFFAPlayer(player.Player);

            if (fPlayer == null)
            {
                return;
            }

            Plugin.Instance.UIManager.ClearPreEndingUI(player);
            Plugin.Instance.UIManager.ClearFFAHUD(player);
            Plugin.Instance.UIManager.ClearVoiceChatUI(player);

            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UIManager.ClearCountdownUI(player);
                fPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }
            else if (GamePhase == EGamePhase.WaitingForPlayers)
            {
                Plugin.Instance.UIManager.ClearWaitingForPlayersUI(player);
            }

            player.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, 0);
            fPlayer.GamePlayer.OnGameLeft();
            Players.Remove(fPlayer);
            PlayersLookup.Remove(fPlayer.GamePlayer.SteamID);

            foreach (var ply in Players)
            {
                Plugin.Instance.UIManager.UpdateFFATopUI(ply, Players);
            }
            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            var fPlayer = GetFFAPlayer(player);
            if (fPlayer == null)
            {
                return;
            }

            if (fPlayer.GamePlayer.HasScoreboard)
            {
                fPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideFFALeaderboard(fPlayer.GamePlayer);
            }

            var victimKS = fPlayer.KillStreak;
            var updatedKiller = cause == EDeathCause.WATER ? fPlayer.GamePlayer.SteamID : (cause == EDeathCause.LANDMINE || cause == EDeathCause.SHRED ? (fPlayer.GamePlayer.LastDamager.Count > 0 ? fPlayer.GamePlayer.LastDamager.Pop() : killer) : killer);

            Logging.Debug($"Game player died, player name: {fPlayer.GamePlayer.Player.CharacterName}, cause: {cause}");
            fPlayer.OnDeath(updatedKiller);
            fPlayer.GamePlayer.OnDeath(updatedKiller, Config.FFA.FileData.RespawnSeconds);

            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(fPlayer.GamePlayer.SteamID, 1));

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
                    Logging.Debug("Player killed themselves, returning");
                    return;
                }

                var questConditions = new Dictionary<EQuestCondition, int>
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
                    var assister = GetFFAPlayer(fPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        assister.Assists++;
                        assister.Score += Config.Points.FileData.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UIManager.ShowXPUI(assister.GamePlayer, Config.Medals.FileData.AssistKillXP, Plugin.Instance.Translate("Assist_Kill", fPlayer.GamePlayer.Player.CharacterName.ToUnrich()));
                        }
                        ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(assister.GamePlayer.SteamID, Config.Medals.FileData.AssistKillXP));
                    }
                    fPlayer.GamePlayer.LastDamager.Clear();
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
                        Logging.Debug($"Player died through melee, setting equipment to {equipmentUsed}");
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
                        Logging.Debug($"Player died through gun, setting equipment to {equipmentUsed}");
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
                        Logging.Debug($"Player died through lethal, setting equipment to {equipmentUsed}");
                        questConditions.Add(EQuestCondition.Gadget, equipmentUsed);
                        break;
                    default:
                        Logging.Debug($"Player died through {cause}, setting equipment to 0");
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

                if (kPlayer.PlayersKilled.ContainsKey(fPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[fPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[fPlayer.GamePlayer.SteamID] > Config.Medals.FileData.DominationKills)
                    {
                        xpGained += Config.Medals.FileData.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                        Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Domination, questConditions);
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
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Revenge, questConditions);
                }

                if (isFirstKill)
                {
                    xpGained += Config.Medals.FileData.FirstKillXP;
                    xpText += Plugin.Instance.Translate("First_Kill").ToRich() + "\n";
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.FirstKill, questConditions);
                }

                Logging.Debug($"Checking if {(fPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude} is greater than {longshotRange}");
                if (cause == EDeathCause.GUN && (fPlayer.GamePlayer.Player.Position - kPlayer.GamePlayer.Player.Position).sqrMagnitude > longshotRange)
                {
                    Logging.Debug("It is and cause is a gun, send longshot range");
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
                kPlayer.GamePlayer.OnKilled(fPlayer.GamePlayer);

                if (equipmentUsed != 0)
                {
                    Logging.Debug($"Sending killfeed with equipment {equipmentUsed}");
                    OnKill(kPlayer.GamePlayer, fPlayer.GamePlayer, equipmentUsed, Config.FFA.FileData.KillFeedHexCode, Config.FFA.FileData.KillFeedHexCode);
                }

                foreach (var ply in Players)
                {
                    Plugin.Instance.UIManager.UpdateFFATopUI(ply, Players);
                }
                if (kPlayer.Kills == Config.FFA.FileData.ScoreLimit)
                {
                    Plugin.Instance.StartCoroutine(GameEnd());
                }

                Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Kill, questConditions);
                Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.MultiKill, questConditions);
                Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Killstreak, questConditions);
                if (limb == ELimb.SKULL && cause == EDeathCause.GUN)
                {
                    Plugin.Instance.QuestManager.CheckQuest(kPlayer.GamePlayer, EQuestType.Headshots, questConditions);
                }
                Plugin.Instance.QuestManager.CheckQuest(fPlayer.GamePlayer, EQuestType.Death, questConditions);

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
            var player = GetFFAPlayer(parameters.player);
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

            var damageReducePercent = player.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageReducePerkName, out LoadoutPerk damageReducerPerk) ? ((float)damageReducerPerk.Perk.SkillLevel / 100) : 0f;
            Logging.Debug($"{player.GamePlayer.Player.CharacterName} got damaged, perk name {damageReducePerkName}, damage reduce percent: {damageReducePercent} damage amount: {parameters.damage}");
            parameters.damage -= damageReducePercent * parameters.damage;
            Logging.Debug($"Damage after perk calculation: {parameters.damage}");

            player.GamePlayer.OnDamaged(parameters.killer);

            var kPlayer = GetFFAPlayer(parameters.killer);
            if (kPlayer == null)
            {
                return;
            }

            var damageIncreasePercent = kPlayer.GamePlayer.ActiveLoadout.PerksSearchByType.TryGetValue(damageIncreasePerkName, out LoadoutPerk damageIncreaserPerk) ? ((float)damageIncreaserPerk.Perk.SkillLevel / 100) : 0f;
            Logging.Debug($"{kPlayer.GamePlayer.Player.CharacterName} damaged someone, damage increase percent {damageIncreasePercent} perk name {damageIncreasePerkName}, damage amount: {parameters.damage}");
            parameters.damage += damageIncreasePercent * parameters.damage;
            Logging.Debug($"Damage after perk calculation: {parameters.damage}");

            if (kPlayer.GamePlayer.HasSpawnProtection)
            {
                kPlayer.GamePlayer.m_RemoveSpawnProtection.Stop();
                kPlayer.GamePlayer.HasSpawnProtection = false;
            }
        }

        public override void OnPlayerRevived(UnturnedPlayer player)
        {
            var fPlayer = GetFFAPlayer(player);
            if (fPlayer == null)
            {
                return;
            }

            fPlayer.GamePlayer.OnRevived(Config.FFA.FileData.Kit, Config.FFA.FileData.TeamGloves);
        }

        public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition)
        {
            if (GetFFAPlayer(player.Player) == null)
            {
                return;
            }

            var spawnPoint = SpawnPoints.Count > 0 ? SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)] : UnavailableSpawnPoints[UnityEngine.Random.Range(0, UnavailableSpawnPoints.Count)];

            if (SpawnPoints.Count > 0)
            {
                Plugin.Instance.StartCoroutine(SpawnUsedUp(spawnPoint));
            }

            respawnPosition = spawnPoint.GetSpawnPoint();
            player.GiveSpawnProtection(Config.FFA.FileData.SpawnProtectionSeconds);
        }

        public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
        {
            var fPlayer = GetFFAPlayer(player.Player);
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
                if (!Plugin.Instance.DBManager.PlayerData.TryGetValue(player.SteamID, out PlayerData data) || data.IsMuted)
                {
                    var expiryTime = data.MuteExpiry.UtcDateTime - DateTime.UtcNow;
                    Utility.Say(player.Player, $"<color=red>You are muted for{(expiryTime.Days == 0 ? "" : $" {expiryTime.Days} Days ")}{(expiryTime.Hours == 0 ? "" : $" {expiryTime.Hours} Hours")} {expiryTime.Minutes} Minutes");
                    return;
                }

                var iconLink = Plugin.Instance.DBManager.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "";
                var updatedText = $"<color={Config.FFA.FileData.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={Config.FFA.FileData.ChatMessageHexCode}>{text.ToUnrich()}</color>";

                foreach (var reciever in Players)
                {
                    ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: iconLink, useRichTextFormatting: true);
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
            Plugin.Instance.LoadoutManager.GiveLoadout(player.GamePlayer, Config.FFA.FileData.Kit, Config.FFA.FileData.TeamGloves);
        }

        public void SpawnPlayer(FFAPlayer player, bool seperateSpawnPoint)
        {
            if (SpawnPoints.Count == 0) return;

            var spawnPoint = seperateSpawnPoint ? SpawnPoints[Players.IndexOf(player)] : (SpawnPoints.Count > 0 ? SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)] : UnavailableSpawnPoints[UnityEngine.Random.Range(0, UnavailableSpawnPoints.Count)]);
            Logging.Debug($"Spawning {player.GamePlayer.Player.CharacterName} on {Location.LocationName}, spawnpoint found: x: {spawnPoint.X}, y: {spawnPoint.Y}, z: {spawnPoint.Z}, yaw: {spawnPoint.Yaw}");
            if (!seperateSpawnPoint && SpawnPoints.Count > 0)
            {
                Plugin.Instance.StartCoroutine(SpawnUsedUp(spawnPoint));
            }
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), spawnPoint.Yaw);
            player.GamePlayer.GiveSpawnProtection(Config.FFA.FileData.SpawnProtectionSeconds);
        }

        public override void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable)
        {
            var fPlayer = GetFFAPlayer(player.Player);
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
            var fPlayer = GetFFAPlayer(player.Player);
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
            player.ScoreboardCooldown = DateTime.UtcNow.AddSeconds(1);

            if (fPlayer.GamePlayer.HasScoreboard)
            {
                fPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideFFALeaderboard(fPlayer.GamePlayer);
            }
            else
            {
                fPlayer.GamePlayer.HasScoreboard = true;
                Plugin.Instance.UIManager.SetupFFALeaderboard(fPlayer, Players, Location, true, IsHardcore);
                Plugin.Instance.UIManager.ShowFFALeaderboard(fPlayer.GamePlayer);
            }
        }

        public override void PlayerStanceChanged(PlayerStance obj)
        {
            var fPlayer = GetFFAPlayer(obj.player);
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
