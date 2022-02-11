﻿using Rocket.Core;
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
using UnturnedBlackout.Database;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models;

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
        public Coroutine SpawnSwitcher { get; set; }

        public uint Frequency { get; set; }
        public TDMGame(ArenaLocation location) : base(EGameType.TDM, location)
        {
            Utility.Debug($"Initializing TDM game for location {location.LocationName}");
            SpawnPoints = new Dictionary<int, List<TDMSpawnPoint>>();
            foreach (var spawnPoint in Plugin.Instance.DataManager.Data.TDMSpawnPoints.Where(k => k.LocationID == location.LocationID))
            {
                if (SpawnPoints.TryGetValue(spawnPoint.GroupID, out List<TDMSpawnPoint> spawnPoints))
                {
                    spawnPoints.Add(spawnPoint);
                } else
                {
                    SpawnPoints.Add(spawnPoint.GroupID, new List<TDMSpawnPoint> { spawnPoint });
                }
            }
            Utility.Debug($"Found {SpawnPoints.Count} spawnpoints registered");
            foreach (var key in SpawnPoints.Keys)
            {
                Utility.Debug(key.ToString());
            }
            Players = new List<TDMPlayer>();
            PlayersLookup = new Dictionary<CSteamID, TDMPlayer>();

            var blueTeamInfo = Config.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
            var redTeamInfo = Config.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

            BlueTeam = new TDMTeam(this, (byte)ETeam.Blue, false, blueTeamInfo);
            RedTeam = new TDMTeam(this, (byte)ETeam.Red, false, redTeamInfo);
            Frequency = Utility.GetFreeFrequency();

            GameStarter = Plugin.Instance.StartCoroutine(StartGame());
        }

        public IEnumerator StartGame()
        {
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
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);

                Plugin.Instance.UIManager.SendTDMHUD(player, BlueTeam, RedTeam);
                Plugin.Instance.UIManager.ClearCountdownUI(player.GamePlayer);
            }

            SpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
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
            } else if (RedTeam.Score > BlueTeam.Score)
            {
                wonTeam = RedTeam;
            } else
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
            for (int index = 0; index < Players.Count; index++)
            {
                var player = Players[index];
                Plugin.Instance.UIManager.ClearTDMHUD(player.GamePlayer);
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
            if (SpawnSwitcher != null)
            {
                Plugin.Instance.StopCoroutine(SpawnSwitcher);
            }

            StartVoting();
        }

        public override void AddPlayerToGame(GamePlayer player)
        {
            Utility.Debug($"Adding {player.Player.CharacterName} to TDM game");
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already in the game, returning");
                return;
            }

            Utility.Debug($"Blue Players: {BlueTeam.Players.Count}, Red Players: {RedTeam.Players.Count}");
            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;

            Utility.Debug($"Found a team for player with id {team.TeamID}");
            TDMPlayer tPlayer = new TDMPlayer(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(tPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, tPlayer);
            GiveLoadout(tPlayer);

            if (GamePhase == EGamePhase.Starting)
            {
                player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UIManager.ShowCountdownUI(player);
                SpawnPlayer(tPlayer);
            }
            else
            {
                Plugin.Instance.UIManager.SendTDMHUD(tPlayer, BlueTeam, RedTeam);
                SpawnPlayer(tPlayer);
            }
            Plugin.Instance.UIManager.SendPreEndingUI(tPlayer.GamePlayer);

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            Utility.Debug($"Removing {player.Player.CharacterName} from TDM game");
            if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already not in the game, returning");
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

            if (tPlayer != null)
            {
                tPlayer.Team.RemovePlayer(tPlayer.GamePlayer.SteamID);
                tPlayer.GamePlayer.OnGameLeft();
                Players.Remove(tPlayer);
                PlayersLookup.Remove(tPlayer.GamePlayer.SteamID);
            }

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            Utility.Debug("Player died, getting the tdm player");
            var tPlayer = GetTDMPlayer(player);
            if (tPlayer == null)
            {
                Utility.Debug("Could'nt find the tdm player, returning");
                return;
            }

            if (tPlayer.GamePlayer.HasScoreboard)
            {
                tPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideTDMLeaderboard(tPlayer.GamePlayer);
            }

            var victimKS = tPlayer.KillStreak;

            Utility.Debug($"Game player found, player name: {tPlayer.GamePlayer.Player.CharacterName}");
            tPlayer.OnDeath(killer);
            tPlayer.GamePlayer.OnDeath(killer);
            tPlayer.Team.OnDeath(tPlayer.GamePlayer.SteamID);

            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(tPlayer.GamePlayer.SteamID, 1));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var kPlayer = GetTDMPlayer(killer);
                if (kPlayer == null)
                {
                    Utility.Debug("Could'nt find the killer, returning");
                    return;
                }

                if (kPlayer.GamePlayer.SteamID == tPlayer.GamePlayer.SteamID)
                {
                    Utility.Debug("Player killed themselves, returning");
                    return;
                }

                Utility.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");
                if (tPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    tPlayer.GamePlayer.LastDamager.Pop();
                }

                if (tPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    var assister = GetTDMPlayer(tPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        Utility.Debug($"Last damage done to the player by {assister.GamePlayer.Player.CharacterName}");
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
                if (cause == EDeathCause.MELEE)
                {
                    xpGained += Config.TDM.XPPerMeleeKill;
                    xpText += Plugin.Instance.Translate("Melee_Kill").ToRich();

                }
                else if (limb == ELimb.SKULL)
                {
                    xpGained += Config.TDM.XPPerKillHeadshot;
                    xpText += Plugin.Instance.Translate("Headshot_Kill").ToRich();
                }
                else
                {
                    xpGained += Config.TDM.XPPerKill;
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

                foreach (var ply in Players)
                {
                    Plugin.Instance.UIManager.UpdateTDMScore(ply, kPlayer.Team);
                }
                if (kPlayer.Team.Score == Config.TDM.ScoreLimit)
                {
                    Plugin.Instance.StartCoroutine(GameEnd(kPlayer.Team));
                }
                OnKill(kPlayer.GamePlayer, tPlayer.GamePlayer, kPlayer.GamePlayer.Player.Player.equipment.itemID, kPlayer.Team.Info.KillFeedHexCode, tPlayer.Team.Info.KillFeedHexCode);

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
            Utility.Debug($"{parameters.player.channel.owner.playerID.characterName} got damaged, checking if the player is in game");
            var player = GetTDMPlayer(parameters.player);
            if (player == null)
            {
                Utility.Debug("Player isn't ingame, returning");
                return;
            }

            if (GamePhase == EGamePhase.Starting || GamePhase == EGamePhase.Ending)
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

            var kPlayer = GetTDMPlayer(parameters.killer);
            if (kPlayer == null)
            {
                Utility.Debug("Killer not found, returning");
                return;
            }

            if (kPlayer.GamePlayer.HasSpawnProtection)
            {
                kPlayer.GamePlayer.HasSpawnProtection = false;
            }
        }

        public override void OnPlayerRevived(UnturnedPlayer player)
        {
            Utility.Debug("Player revived, getting the tdm player");
            var tPlayer = GetTDMPlayer(player);
            if (tPlayer == null)
            {
                Utility.Debug("Could'nt find the tdm player, returning");
                return;
            }

            Utility.Debug($"Game player found, player name: {tPlayer.GamePlayer.Player.CharacterName}");
            Utility.Debug("Reviving the player");

            tPlayer.GamePlayer.OnRevived();
            SpawnPlayer(tPlayer);
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
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(player.SteamID, out PlayerData data))
                {
                    return;
                }

                var iconLink = Plugin.Instance.UIManager.Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Plugin.Instance.UIManager.Icons.TryGetValue(0, out icon) ? icon.IconLink28 : "");
                var updatedText = $"<color={tPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName.Trim()}</color>: <color={tPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

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
            Utility.Debug($"Giving loadout to {player.GamePlayer.Player.CharacterName}");

            player.GamePlayer.Player.Player.inventory.ClearInventory();
            R.Commands.Execute(player.GamePlayer.Player, $"/kit {player.Team.Info.KitNames[UnityEngine.Random.Range(0, player.Team.Info.KitNames.Count)]}");
        }

        public void SpawnPlayer(TDMPlayer player)
        {
            Utility.Debug($"Spawning {player.GamePlayer.Player.CharacterName}, getting a random location");
            if (!SpawnPoints.TryGetValue(player.Team.SpawnPoint, out var spawnPoints))
            {
                Utility.Debug($"Could'nt find the spawnpoints for group {player.Team.SpawnPoint}");
                return;
            }

            if (spawnPoints.Count == 0)
            {
                Utility.Debug("No spawnpoints set for TDM, returning");
                return;
            }

            var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), 0);
            player.GamePlayer.GiveSpawnProtection(Config.TDM.SpawnProtectionSeconds);
        }

        public override void PlayerLeaned(PlayerAnimator obj)
        {
            if (obj.lean != 1) return;
            TDMPlayer tPlayer = GetTDMPlayer(obj.player);
            if (tPlayer == null) return;
            if (GamePhase == EGamePhase.Ending || GamePhase == EGamePhase.Starting) return;
            Utility.Debug($"{obj.player.channel.owner.playerID.characterName} leaned, lean {obj.lean}");

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
            Utility.Debug($"{tPlayer.GamePlayer.Player.CharacterName} changed stance to {obj.stance}");
            tPlayer.GamePlayer.OnStanceChanged(obj.stance);
        }

        public IEnumerator SpawnSwitch()
        {
            yield return new WaitForSeconds(Config.SpawnSwitchSeconds);
            SwitchSpawn();
        }

        public void SwitchSpawn()
        {
            Utility.Debug($"Switching the spawns, current spawn {BlueTeam.SpawnPoint} for blue, {RedTeam.SpawnPoint} for red");
            if (SpawnSwitcher != null)
            {
                Plugin.Instance.StopCoroutine(SpawnSwitcher);
            }
            var keys = SpawnPoints.Keys.ToList();
            Utility.Debug($"Found {keys.Count} spawn groups to switch from");
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
            Utility.Debug($"Changed spawns, current spawn {BlueTeam.SpawnPoint} for blue, {RedTeam.SpawnPoint} for red");
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
    }
}
