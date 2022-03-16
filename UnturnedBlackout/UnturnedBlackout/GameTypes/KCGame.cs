using Rocket.Core;
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

        public KCGame(ArenaLocation location) : base(EGameType.KC, location)
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
            Players = new List<KCPlayer>();
            PlayersLookup = new Dictionary<CSteamID, KCPlayer>();

            var blueTeamInfo = Config.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
            var redTeamInfo = Config.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

            BlueTeam = new KCTeam(this, (byte)ETeam.Blue, false, Config.KC.BlueDogTagID, blueTeamInfo);
            RedTeam = new KCTeam(this, (byte)ETeam.Red, false, Config.KC.RedDogTagID, redTeamInfo);
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
            }

            for (int seconds = Config.KC.StartSeconds; seconds >= 0; seconds--)
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
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);

                Plugin.Instance.UIManager.SendKCHUD(player, BlueTeam, RedTeam);
                Plugin.Instance.UIManager.ClearCountdownUI(player.GamePlayer);
            }

            m_SpawnSwitcher.Start();
            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.KC.EndSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.UpdateKCTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
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
            Plugin.Instance.UIManager.OnGameUpdated(this);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ClearKCHUD(player.GamePlayer);
                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UIManager.HideKCLeaderboard(player.GamePlayer);
                }
                if (player.Team == wonTeam)
                {
                    var xp = player.XP * Config.KC.WinMultipler;
                    ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(player.GamePlayer.SteamID, (uint)xp));
                }
                Plugin.Instance.UIManager.SetupPreEndingUI(player.GamePlayer, EGameType.KC, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName);
                player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UIManager.SetupKCLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false);
                WipeItems();
            });
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ShowKCLeaderboard(player.GamePlayer);
            }
            yield return new WaitForSeconds(Config.EndingLeaderboardSeconds);
            foreach (var player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.GameManager.SendPlayerToLobby(player.GamePlayer.Player);
            }

            Players = new List<KCPlayer>();
            BlueTeam.Destroy();
            RedTeam.Destroy();
            if (m_SpawnSwitcher.Enabled)
            {
                m_SpawnSwitcher.Stop();
            }

            var gameModes = new List<byte> { (byte)EGameType.CTF, (byte)EGameType.FFA, (byte)EGameType.TDM, (byte)EGameType.KC };
            gameModes.Remove((byte)GameMode);
            var gameMode = (EGameType)gameModes[UnityEngine.Random.Range(0, gameModes.Count)];
            GamePhase = EGamePhase.Ended;
            Plugin.Instance.GameManager.EndGame(this);
            Plugin.Instance.GameManager.StartGame(Location, gameMode);
        }

        public override IEnumerator AddPlayerToGame(GamePlayer player)
        {
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                yield break;
            }
            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;
            KCPlayer kPlayer = new KCPlayer(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(kPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, kPlayer);

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
            Plugin.Instance.UIManager.SendLoadingUI(player, GameMode, Location);
            yield return new WaitForSeconds(5);

            GiveLoadout(kPlayer);
            Plugin.Instance.UIManager.SendPreEndingUI(kPlayer.GamePlayer);
            SpawnPlayer(kPlayer);
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
                            if (ply == kPlayer)
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
                    Plugin.Instance.UIManager.SetupKCLeaderboard(kPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true);
                    Plugin.Instance.UIManager.ShowKCLeaderboard(kPlayer.GamePlayer);
                    break;
                default:
                    Plugin.Instance.UIManager.SendKCHUD(kPlayer, BlueTeam, RedTeam);
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

            Plugin.Instance.UIManager.ClearKCHUD(player);
            Plugin.Instance.UIManager.ClearPreEndingUI(player);
            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UIManager.ClearCountdownUI(player);
                kPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }
            else if (GamePhase == EGamePhase.WaitingForPlayers)
            {
                Plugin.Instance.UIManager.ClearWaitingForPlayersUI(player);
            }

            if (kPlayer != null)
            {
                kPlayer.Team.RemovePlayer(kPlayer.GamePlayer.SteamID);
                kPlayer.GamePlayer.OnGameLeft();
                Players.Remove(kPlayer);
                PlayersLookup.Remove(kPlayer.GamePlayer.SteamID);
            }

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            var vPlayer = GetKCPlayer(player);
            if (vPlayer == null)
            {
                return;
            }

            if (vPlayer.GamePlayer.HasScoreboard)
            {
                vPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideKCLeaderboard(vPlayer.GamePlayer);
            }

            var victimKS = vPlayer.KillStreak;
            Logging.Debug($"Game player died, player name: {vPlayer.GamePlayer.Player.CharacterName}");
            vPlayer.OnDeath(killer);
            vPlayer.GamePlayer.OnDeath(killer, Config.KC.RespawnSeconds);
            vPlayer.Team.OnDeath(vPlayer.GamePlayer.SteamID);
            ItemManager.dropItem(new Item(vPlayer.Team.DogTagID, true), vPlayer.GamePlayer.Player.Player.transform.position, true, true, true);
            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(vPlayer.GamePlayer.SteamID, 1));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var kPlayer = GetKCPlayer(killer);
                if (kPlayer == null)
                {
                    return;
                }

                if (kPlayer.GamePlayer.SteamID == vPlayer.GamePlayer.SteamID)
                {
                    Logging.Debug("Player killed themselves, returning");
                    return;
                }

                Logging.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");
                if (vPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    vPlayer.GamePlayer.LastDamager.Pop();
                }

                if (vPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    var assister = GetKCPlayer(vPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        assister.Assists++;
                        assister.Score += Config.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UIManager.ShowXPUI(assister.GamePlayer, Config.KC.XPPerAssist, Plugin.Instance.Translate("Assist_Kill", vPlayer.GamePlayer.Player.CharacterName.ToUnrich()).ToRich());
                        }
                        ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(assister.GamePlayer.SteamID, (uint)Config.KC.XPPerAssist));
                    }
                    vPlayer.GamePlayer.LastDamager.Clear();
                }

                kPlayer.Kills++;
                kPlayer.Score += Config.KillPoints;

                int xpGained = 0;
                string xpText = "";
                if (cause == EDeathCause.MELEE)
                {
                    xpGained += Config.KC.XPPerMeleeKill;
                    xpText += Plugin.Instance.Translate("Melee_Kill").ToRich();

                }
                else if (limb == ELimb.SKULL)
                {
                    xpGained += Config.KC.XPPerKillHeadshot;
                    xpText += Plugin.Instance.Translate("Headshot_Kill").ToRich();
                }
                else
                {
                    xpGained += Config.KC.XPPerKill;
                    xpText += Plugin.Instance.Translate("Normal_Kill").ToRich();
                }
                xpText += "\n";

                kPlayer.KillStreak++;

                if (kPlayer.MultipleKills == 0)
                {
                    kPlayer.MultipleKills++;
                }
                else if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
                {
                    xpGained += Config.KC.BaseXPMK + (++kPlayer.MultipleKills * Config.KC.IncreaseXPPerMK);
                    var multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
                    xpText += (multiKillText == $"Multiple_Kills_Show_{kPlayer.MultipleKills}" ? Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich() : multiKillText) + "\n";
                }
                else
                {
                    kPlayer.MultipleKills = 1;
                }

                if (victimKS > Config.ShutdownKillStreak)
                {
                    xpGained += Config.KC.ShutdownXP;
                    xpText += Plugin.Instance.Translate("Shutdown_Kill").ToRich() + "\n";
                }

                if (kPlayer.PlayersKilled.ContainsKey(vPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[vPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[vPlayer.GamePlayer.SteamID] > Config.DominationKills)
                    {
                        xpGained += Config.KC.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                    }
                }
                else
                {
                    kPlayer.PlayersKilled.Add(vPlayer.GamePlayer.SteamID, 1);
                }
                kPlayer.LastKill = DateTime.UtcNow;
                kPlayer.XP += xpGained;

                Players.Sort((x, y) => y.Kills.CompareTo(x.Kills));

                Plugin.Instance.UIManager.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);
                Plugin.Instance.UIManager.SendMultiKillSound(kPlayer.GamePlayer, kPlayer.MultipleKills);
                kPlayer.CheckKills();
                OnKill(kPlayer.GamePlayer, vPlayer.GamePlayer, kPlayer.GamePlayer.Player.Player.equipment.itemID, kPlayer.Team.Info.KillFeedHexCode, vPlayer.Team.Info.KillFeedHexCode);
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    if (limb == ELimb.SKULL)
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerHeadshotKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    else
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, (uint)xpGained);
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

            if (parameters.cause == EDeathCause.MELEE)
            {
                parameters.damage = 200;
            }

            player.GamePlayer.OnDamaged(parameters.killer);

            var kPlayer = GetKCPlayer(parameters.killer);
            if (kPlayer == null)
            {
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
            var kPlayer = GetKCPlayer(player);
            if (kPlayer == null)
            {
                return;
            }

            kPlayer.GamePlayer.OnRevived();
        }

        public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition)
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
            player.GiveSpawnProtection(Config.KC.SpawnProtectionSeconds);
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
                if (!Plugin.Instance.DBManager.PlayerData.TryGetValue(player.SteamID, out PlayerData data))
                {
                    return;
                }

                var iconLink = Plugin.Instance.DBManager.Levels.TryGetValue((int)data.Level, out XPLevel level) ? level.IconLinkSmall : "";
                var updatedText = $"<color={kPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={kPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

                if (chatMode == EChatMode.GLOBAL)
                {
                    foreach (var reciever in Players)
                    {
                        ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: iconLink, useRichTextFormatting: true);
                    }
                    return;
                }

                var teamPlayers = Players.Where(k => k.Team == kPlayer.Team);
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

            if (kPlayer.Team.DogTagID == P.item.id)
            {
                kPlayer.Score += Config.KillDeniedPoints;
                kPlayer.XP += Config.KC.XPPerKillDenied;
                kPlayer.KillsDenied++;
                Plugin.Instance.UIManager.ShowXPUI(kPlayer.GamePlayer, Config.KC.XPPerKillDenied, Plugin.Instance.Translate("Kill_Denied").ToRich());
                Plugin.Instance.UIManager.SendKillConfirmedSound(kPlayer.GamePlayer);
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, (uint)Config.KC.XPPerKillDenied);
                    await Plugin.Instance.DBManager.IncreasePlayerKillsDeniedAsync(kPlayer.GamePlayer.SteamID, 1);
                });

            }
            else if (P.item.id == otherTeam.DogTagID)
            {
                kPlayer.Score += Config.KillConfirmedPoints;
                kPlayer.XP += Config.KC.XPPerKillConfirmed;
                kPlayer.KillsConfirmed++;
                kPlayer.Team.Score++;
                Plugin.Instance.UIManager.ShowXPUI(kPlayer.GamePlayer, Config.KC.XPPerKillConfirmed, Plugin.Instance.Translate("Kill_Confirmed").ToRich());
                Plugin.Instance.UIManager.SendKillDeniedSound(kPlayer.GamePlayer);
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, (uint)Config.KC.XPPerKillConfirmed);
                    await Plugin.Instance.DBManager.IncreasePlayerKillsConfirmedAsync(kPlayer.GamePlayer.SteamID, 1);
                });

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    foreach (var ply in Players)
                    {
                        Plugin.Instance.UIManager.UpdateKCScore(ply, kPlayer.Team);
                    }
                    if (kPlayer.Team.Score == Config.KC.ScoreLimit)
                    {
                        Plugin.Instance.StartCoroutine(GameEnd(kPlayer.Team));
                    }
                });
            }
            else
            {
                return;
            }

            player.Player.inventory.removeItem((byte)inventoryGroup, inventoryIndex);
        }

        public void GiveLoadout(KCPlayer player)
        {
            player.GamePlayer.Player.Player.inventory.ClearInventory();
            Plugin.Instance.LoadoutManager.GiveLoadout(player.GamePlayer, player.Team.Info.TeamKits[UnityEngine.Random.Range(0, player.Team.Info.TeamKits.Count)]);
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
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), 0);
            player.GamePlayer.GiveSpawnProtection(Config.KC.SpawnProtectionSeconds);
        }

        public override void PlayerChangeFiremode(GamePlayer player)
        {
            KCPlayer kPlayer = GetKCPlayer(player.Player);
            if (kPlayer == null)
            {
                return;
            }

            if (GamePhase == EGamePhase.Ending || GamePhase == EGamePhase.Starting)
            {
                return;
            }

            if (kPlayer.GamePlayer.HasScoreboard)
            {
                kPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideKCLeaderboard(kPlayer.GamePlayer);
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
                Plugin.Instance.UIManager.SetupKCLeaderboard(kPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true);
                Plugin.Instance.UIManager.ShowKCLeaderboard(kPlayer.GamePlayer);
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

