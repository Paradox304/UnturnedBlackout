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
        public Timer m_SpawnSwitcher { get; set; }

        public uint Frequency { get; set; }

        public TDMGame(ArenaLocation location) : base(EGameType.TDM, location)
        {
            SpawnPoints = new Dictionary<int, List<TDMSpawnPoint>>();
            foreach (var spawnPoint in Plugin.Instance.DataManager.Data.TDMSpawnPoints.Where(k => k.LocationID == location.LocationID))
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

            var blueTeamInfo = Config.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
            var redTeamInfo = Config.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

            BlueTeam = new TDMTeam(this, (byte)ETeam.Blue, false, blueTeamInfo);
            RedTeam = new TDMTeam(this, (byte)ETeam.Red, false, redTeamInfo);
            Frequency = Utility.GetFreeFrequency();

            m_SpawnSwitcher = new Timer(Config.SpawnSwitchSeconds * 1000);
            m_SpawnSwitcher.Elapsed += SpawnSwitch;
        }

        public IEnumerator StartGame()
        {
            TaskDispatcher.QueueOnMainThread(() => WipeItems());
            GamePhase = EGamePhase.Starting;
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ClearWaitingForPlayersUI(player.GamePlayer);
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UIManager.ShowCountdownUI(player.GamePlayer);
                SpawnPlayer(player);
            }

            for (int seconds = Config.TDM.StartSeconds; seconds >= 0; seconds--)
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

                Plugin.Instance.UIManager.SendTDMHUD(player, BlueTeam, RedTeam);
                Plugin.Instance.UIManager.ClearCountdownUI(player.GamePlayer);
            }

            m_SpawnSwitcher.Start();
            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.TDM.EndSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.UpdateTDMTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
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
            Plugin.Instance.UIManager.OnGameUpdated(this);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ClearTDMHUD(player.GamePlayer);
                Plugin.Instance.UIManager.ClearMidgameLoadoutUI(player.GamePlayer);

                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UIManager.HideTDMLeaderboard(player.GamePlayer);
                }
                if (player.Team == wonTeam)
                {
                    var xp = player.XP * Config.TDM.WinMultipler;
                    ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(player.GamePlayer.SteamID, (uint)xp));
                }
                Plugin.Instance.UIManager.SetupPreEndingUI(player.GamePlayer, EGameType.TDM, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName);
                player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UIManager.SetupTDMLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false);
                WipeItems();
            });
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ShowTDMLeaderboard(player.GamePlayer);
            }
            yield return new WaitForSeconds(Config.EndingLeaderboardSeconds);
            foreach (var player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.GameManager.SendPlayerToLobby(player.GamePlayer.Player);
            }

            Players = new List<TDMPlayer>();
            BlueTeam.Destroy();
            RedTeam.Destroy();
            if (m_SpawnSwitcher.Enabled)
            {
                m_SpawnSwitcher.Stop();
            }

            var gameModes = new List<byte> { (byte)EGameType.CTF, (byte)EGameType.FFA, (byte)EGameType.TDM, (byte)EGameType.KC };
            gameModes.Remove((byte)GameMode);
            var gameMode = (EGameType)gameModes[UnityEngine.Random.Range(0, gameModes.Count)];
            var locations = Plugin.Instance.GameManager.AvailableLocations.ToList();
            locations.Add(Location.LocationID);
            var randomLocation = locations[UnityEngine.Random.Range(0, locations.Count)];
            var location = Config.ArenaLocations.FirstOrDefault(k => k.LocationID == randomLocation);
            GamePhase = EGamePhase.Ended;
            Plugin.Instance.GameManager.EndGame(this);
            Plugin.Instance.GameManager.StartGame(location ?? Location, gameMode);
        }

        public override IEnumerator AddPlayerToGame(GamePlayer player)
        {
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                yield break;
            }

            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;
            TDMPlayer tPlayer = new TDMPlayer(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(tPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, tPlayer);

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
            Plugin.Instance.UIManager.SendLoadingUI(player, GameMode, Location);
            yield return new WaitForSeconds(5);
            var currentPos = player.Player.Position;
            player.Player.Player.teleportToLocationUnsafe(new Vector3(currentPos.x, currentPos.y + 100, currentPos.z), 0);
            GiveLoadout(tPlayer);
            Plugin.Instance.UIManager.SendPreEndingUI(tPlayer.GamePlayer);
            SpawnPlayer(tPlayer);
            Plugin.Instance.UIManager.ClearLoadingUI(player);
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
                            if (ply == tPlayer)
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
                    Plugin.Instance.UIManager.SetupTDMLeaderboard(tPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true);
                    Plugin.Instance.UIManager.ShowTDMLeaderboard(tPlayer.GamePlayer);
                    break;
                default:
                    Plugin.Instance.UIManager.SendTDMHUD(tPlayer, BlueTeam, RedTeam);
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

            Plugin.Instance.UIManager.ClearTDMHUD(player);
            Plugin.Instance.UIManager.ClearPreEndingUI(player);

            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UIManager.ClearCountdownUI(player);
                tPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }
            else if (GamePhase == EGamePhase.WaitingForPlayers)
            {
                Plugin.Instance.UIManager.ClearWaitingForPlayersUI(player);
                foreach (var ply in Players)
                {
                    if (ply == tPlayer)
                    {
                        continue;
                    }

                    Plugin.Instance.UIManager.UpdateWaitingForPlayersUI(ply.GamePlayer, Players.Count - 1, Location.GetMinPlayers(GameMode));
                }
            }

            tPlayer.Team.RemovePlayer(tPlayer.GamePlayer.SteamID);
            tPlayer.GamePlayer.OnGameLeft();
            Players.Remove(tPlayer);
            PlayersLookup.Remove(tPlayer.GamePlayer.SteamID);

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            var tPlayer = GetTDMPlayer(player);
            if (tPlayer == null)
            {
                return;
            }

            if (tPlayer.GamePlayer.HasScoreboard)
            {
                tPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideTDMLeaderboard(tPlayer.GamePlayer);
            }

            var victimKS = tPlayer.KillStreak;
            var updatedKiller = cause == EDeathCause.LANDMINE ? (tPlayer.GamePlayer.LastDamager.Count > 0 ? tPlayer.GamePlayer.LastDamager.Pop() : killer) : killer;

            Logging.Debug($"Game player died, player name: {tPlayer.GamePlayer.Player.CharacterName}");
            tPlayer.OnDeath(updatedKiller);
            tPlayer.GamePlayer.OnDeath(updatedKiller, Config.TDM.RespawnSeconds);
            tPlayer.Team.OnDeath(tPlayer.GamePlayer.SteamID);

            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(tPlayer.GamePlayer.SteamID, 1));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var kPlayer = GetTDMPlayer(updatedKiller);
                if (kPlayer == null)
                {
                    return;
                }

                if (kPlayer.GamePlayer.SteamID == tPlayer.GamePlayer.SteamID)
                {
                    Logging.Debug("Player killed themselves, returning");
                    return;
                }

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
                        assister.Score += Config.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UIManager.ShowXPUI(assister.GamePlayer, Config.TDM.XPPerAssist, Plugin.Instance.Translate("Assist_Kill", tPlayer.GamePlayer.Player.CharacterName.ToUnrich()));
                        }
                        ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(assister.GamePlayer.SteamID, (uint)Config.TDM.XPPerAssist));
                    }
                    tPlayer.GamePlayer.LastDamager.Clear();
                }

                kPlayer.Kills++;
                kPlayer.Team.Score++;
                kPlayer.Score += Config.KillPoints;

                int xpGained = 0;
                string xpText = "";
                ushort equipmentUsed = 0;

                switch (cause)
                {
                    case EDeathCause.MELEE:
                        xpGained += Config.TDM.XPPerMeleeKill;
                        xpText += Plugin.Instance.Translate("Melee_Kill").ToRich();
                        equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Knife?.Knife?.KnifeID ?? 0;
                        break;
                    case EDeathCause.GUN:
                        if (limb == ELimb.SKULL)
                        {
                            xpGained += Config.TDM.XPPerKillHeadshot;
                            xpText += Plugin.Instance.Translate("Headshot_Kill").ToRich();
                        }
                        else
                        {
                            xpGained += Config.TDM.XPPerKill;
                            xpText += Plugin.Instance.Translate("Normal_Kill").ToRich();
                        }
                        equipmentUsed = kPlayer.GamePlayer.Player.Player.equipment.itemID;
                        break;
                    case EDeathCause.CHARGE:
                    case EDeathCause.GRENADE:
                    case EDeathCause.LANDMINE:
                    case EDeathCause.BURNING:
                        xpGained += Config.TDM.XPPerLethalKill;
                        xpText += Plugin.Instance.Translate("Lethal_Kill").ToRich();
                        equipmentUsed = kPlayer.GamePlayer.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0;
                        break;
                    default:
                        break;
                }
                xpText += "\n";

                kPlayer.KillStreak++;

                if (kPlayer.MultipleKills == 0)
                {
                    kPlayer.MultipleKills++;
                }
                else if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
                {
                    xpGained += Config.TDM.BaseXPMK + (++kPlayer.MultipleKills * Config.TDM.IncreaseXPPerMK);
                    var multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
                    xpText += (multiKillText == $"Multiple_Kills_Show_{kPlayer.MultipleKills}" ? Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich() : multiKillText) + "\n";
                }
                else
                {
                    kPlayer.MultipleKills = 1;
                }

                if (victimKS > Config.ShutdownKillStreak)
                {
                    xpGained += Config.TDM.ShutdownXP;
                    xpText += Plugin.Instance.Translate("Shutdown_Kill").ToRich() + "\n";
                }

                if (kPlayer.PlayersKilled.ContainsKey(tPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[tPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[tPlayer.GamePlayer.SteamID] > Config.DominationKills)
                    {
                        xpGained += Config.TDM.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                    }
                }
                else
                {
                    kPlayer.PlayersKilled.Add(tPlayer.GamePlayer.SteamID, 1);
                }
                kPlayer.LastKill = DateTime.UtcNow;
                kPlayer.XP += xpGained;
                Players.Sort((x, y) => y.Kills.CompareTo(x.Kills));

                Plugin.Instance.UIManager.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);
                Plugin.Instance.UIManager.SendMultiKillSound(kPlayer.GamePlayer, kPlayer.MultipleKills);
                kPlayer.CheckKills();
                kPlayer.GamePlayer.OnKilled(tPlayer.GamePlayer);

                foreach (var ply in Players)
                {
                    Plugin.Instance.UIManager.UpdateTDMScore(ply, kPlayer.Team);
                }
                if (kPlayer.Team.Score == Config.TDM.ScoreLimit)
                {
                    Plugin.Instance.StartCoroutine(GameEnd(kPlayer.Team));
                }
                if (equipmentUsed != 0)
                {
                    OnKill(kPlayer.GamePlayer, tPlayer.GamePlayer, kPlayer.GamePlayer.Player.Player.equipment.itemID, kPlayer.Team.Info.KillFeedHexCode, tPlayer.Team.Info.KillFeedHexCode);
                }

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
                    await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, (uint)xpGained);
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
            var player = GetTDMPlayer(parameters.player);
            if (player == null)
            {
                return;
            }

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

            if (kPlayer.GamePlayer.HasSpawnProtection)
            {
                kPlayer.GamePlayer.m_RemoveSpawnProtection.Stop();
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

            tPlayer.GamePlayer.OnRevived(tPlayer.Team.Info.TeamKits[UnityEngine.Random.Range(0, tPlayer.Team.Info.TeamKits.Count)]);
        }

        public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition)
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
            player.GiveSpawnProtection(Config.TDM.SpawnProtectionSeconds);
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
                if (!Plugin.Instance.DBManager.PlayerData.TryGetValue(player.SteamID, out PlayerData data) || data.IsMuted)
                {
                    var expiryTime = data.MuteExpiry.UtcDateTime - DateTime.UtcNow;
                    Utility.Say(player.Player, $"<color=red>You are muted for{(expiryTime.Days == 0 ? "" : $" {expiryTime.Days} Days ")}{(expiryTime.Hours == 0 ? "" : $" {expiryTime.Hours} Hours")} {expiryTime.Minutes} Minutes");
                    return;
                }

                var iconLink = Plugin.Instance.DBManager.Levels.TryGetValue((int)data.Level, out XPLevel level) ? level.IconLinkSmall : "";
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

            var tPlayer = GetTDMPlayer(player.Player);
            SendVoiceChat(Players.Where(k => k.Team == tPlayer.Team).Select(k => k.GamePlayer).ToList(), true);
        }

        public void GiveLoadout(TDMPlayer player)
        {
            player.GamePlayer.Player.Player.inventory.ClearInventory();
            Plugin.Instance.LoadoutManager.GiveLoadout(player.GamePlayer, player.Team.Info.TeamKits[UnityEngine.Random.Range(0, player.Team.Info.TeamKits.Count)]);
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
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), 0);
            player.GamePlayer.GiveSpawnProtection(Config.TDM.SpawnProtectionSeconds);
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

            if (GamePhase != EGamePhase.Started)
            {
                return;
            }

            if (tPlayer.GamePlayer.HasScoreboard)
            {
                tPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideTDMLeaderboard(tPlayer.GamePlayer);
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
                Plugin.Instance.UIManager.SetupTDMLeaderboard(tPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true);
                Plugin.Instance.UIManager.ShowTDMLeaderboard(tPlayer.GamePlayer);
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
            if (IsPlayerIngame(player.SteamID) && GamePhase == EGamePhase.Started)
            {
                player.GiveMovement(player.Player.Player.equipment.useable is UseableGun gun && gun.isAiming, false, true);
            }
        }

        public override void PlayerAimingChanged(GamePlayer player, bool isAiming)
        {
            if (IsPlayerIngame(player.SteamID) && GamePhase == EGamePhase.Started)
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
            var keys = SpawnPoints.Keys.ToList();
            if (keys.Count == 0)
            {
                return;
            }
            var sp = BlueTeam.SpawnPoint;
            keys.Remove(sp);
            BlueTeam.SpawnPoint = keys[UnityEngine.Random.Range(0, keys.Count)];
            keys.Add(sp);
            keys.Remove(BlueTeam.SpawnPoint);
            keys.Remove(RedTeam.SpawnPoint);
            RedTeam.SpawnPoint = keys[UnityEngine.Random.Range(0, keys.Count)];
            m_SpawnSwitcher.Start();

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
