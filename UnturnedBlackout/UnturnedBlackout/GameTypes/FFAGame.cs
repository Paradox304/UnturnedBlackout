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
using UnturnedBlackout.Database;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.Level;

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

        public FFAGame(ArenaLocation location) : base(EGameType.FFA, location)
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
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ClearWaitingForPlayersUI(player.GamePlayer);
            }

            for (int seconds = Config.FFA.StartSeconds; seconds >= 0; seconds--)
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

                Plugin.Instance.UIManager.SendFFAHUD(player.GamePlayer);
                Plugin.Instance.UIManager.ClearCountdownUI(player.GamePlayer);
                Plugin.Instance.UIManager.UpdateFFATopUI(player, Players);
            }

            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.FFA.EndSeconds; seconds >= 0; seconds--)
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
            Plugin.Instance.UIManager.OnGameUpdated(this);
            for (int index = 0; index < Players.Count; index++)
            {
                var player = Players[index];
                Plugin.Instance.UIManager.ClearFFAHUD(player.GamePlayer);
                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UIManager.HideFFALeaderboard(player.GamePlayer);
                }
                Plugin.Instance.UIManager.SetupPreEndingUI(player.GamePlayer, EGameType.FFA, index == 0, 0, 0, "", "");
                if (index == 0)
                {
                    var xp = player.XP * Config.FFA.WinMultipler;
                    ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(player.GamePlayer.SteamID, (uint)xp));
                }
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UIManager.SetupFFALeaderboard(Players, Location, false);
                WipeItems();
            });
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ShowFFALeaderboard(player.GamePlayer);
            }
            yield return new WaitForSeconds(Config.EndingLeaderboardSeconds);
            foreach (var player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.GameManager.SendPlayerToLobby(player.GamePlayer.Player);
            }

            Players = new List<FFAPlayer>();
            StartVoting();
        }

        public override IEnumerator AddPlayerToGame(GamePlayer player)
        {
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                yield break;
            }

            FFAPlayer fPlayer = new FFAPlayer(player);

            player.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            Players.Add(fPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, fPlayer);
            Plugin.Instance.UIManager.OnGameCountUpdated(this);

            Plugin.Instance.UIManager.SendLoadingUI(player, GameMode, Location);
            yield return new WaitForSeconds(10);
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
                            Plugin.Instance.UIManager.SendWaitingForPlayersUI(ply.GamePlayer, Players.Count, minPlayers);
                        }
                    }
                    break;
                case EGamePhase.Starting:
                    player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                    Plugin.Instance.UIManager.ShowCountdownUI(player);
                    SpawnPlayer(fPlayer, true);
                    break;
                case EGamePhase.Ending:
                    Plugin.Instance.UIManager.SetupFFALeaderboard(fPlayer, Players, Location, true);
                    Plugin.Instance.UIManager.ShowFFALeaderboard(fPlayer.GamePlayer);
                    break;
                default:
                    Plugin.Instance.UIManager.SendFFAHUD(player);
                    Plugin.Instance.UIManager.UpdateFFATopUI(fPlayer, Players);
                    SpawnPlayer(fPlayer, false);
                    break;
            }

            Plugin.Instance.UIManager.ClearLoadingUI(player);
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                return;
            }

            var fPlayer = GetFFAPlayer(player.Player);

            Plugin.Instance.UIManager.ClearPreEndingUI(player);
            Plugin.Instance.UIManager.ClearFFAHUD(player);

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

            Utility.Debug($"Game player died, player name: {fPlayer.GamePlayer.Player.CharacterName}");
            fPlayer.OnDeath(killer);
            fPlayer.GamePlayer.OnDeath(killer, Config.FFA.RespawnSeconds);

            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(fPlayer.GamePlayer.SteamID, 1));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var kPlayer = GetFFAPlayer(killer);
                if (kPlayer == null)
                {
                    return;
                }

                if (kPlayer.GamePlayer.SteamID == fPlayer.GamePlayer.SteamID)
                {
                    Utility.Debug("Player killed themselves, returning");
                    return;
                }

                Utility.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");
                if (fPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    fPlayer.GamePlayer.LastDamager.Pop();
                }

                if (fPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    var assister = GetFFAPlayer(fPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        assister.Assists++;
                        assister.Score += Config.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UIManager.ShowXPUI(assister.GamePlayer, Config.FFA.XPPerAssist, Plugin.Instance.Translate("Assist_Kill", fPlayer.GamePlayer.Player.CharacterName.ToUnrich()));
                        }
                        ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(assister.GamePlayer.SteamID, (uint)Config.FFA.XPPerAssist));
                    }
                    fPlayer.GamePlayer.LastDamager.Clear();
                }

                kPlayer.Kills++;
                kPlayer.Score += Config.KillPoints;

                int xpGained = 0;
                string xpText = "";
                if (cause == EDeathCause.MELEE)
                {
                    xpGained += Config.FFA.XPPerMeleeKill;
                    xpText += Plugin.Instance.Translate("Melee_Kill").ToRich();

                }
                else if (limb == ELimb.SKULL)
                {
                    xpGained += Config.FFA.XPPerKillHeadshot;
                    xpText += Plugin.Instance.Translate("Headshot_Kill").ToRich();
                }
                else
                {
                    xpGained += Config.FFA.XPPerKill;
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
                    xpGained += Config.FFA.BaseXPMK + (++kPlayer.MultipleKills * Config.FFA.IncreaseXPPerMK);
                    var multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
                    xpText += (multiKillText == $"Multiple_Kills_Show_{kPlayer.MultipleKills}" ? Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich() : multiKillText) + "\n";
                }
                else
                {
                    kPlayer.MultipleKills = 1;
                }

                if (victimKS > Config.ShutdownKillStreak)
                {
                    xpGained += Config.FFA.ShutdownXP;
                    xpText += Plugin.Instance.Translate("Shutdown_Kill").ToRich() + "\n";
                }

                if (kPlayer.PlayersKilled.ContainsKey(fPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[fPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[fPlayer.GamePlayer.SteamID] > Config.DominationKills)
                    {
                        xpGained += Config.FFA.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                    }
                }
                else
                {
                    kPlayer.PlayersKilled.Add(fPlayer.GamePlayer.SteamID, 1);
                }
                kPlayer.LastKill = DateTime.UtcNow;
                kPlayer.XP += xpGained;

                Players.Sort((x, y) => y.Kills.CompareTo(x.Kills));

                Plugin.Instance.UIManager.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);
                Plugin.Instance.UIManager.SendMultiKillSound(kPlayer.GamePlayer, kPlayer.MultipleKills);
                kPlayer.CheckKills();
                OnKill(kPlayer.GamePlayer, fPlayer.GamePlayer, kPlayer.GamePlayer.Player.Player.equipment.itemID, Config.FFA.KillFeedHexCode, Config.FFA.KillFeedHexCode);
                foreach (var ply in Players)
                {
                    Plugin.Instance.UIManager.UpdateFFATopUI(ply, Players);
                }
                if (kPlayer.Kills == Config.FFA.ScoreLimit)
                {
                    Plugin.Instance.StartCoroutine(GameEnd());
                }
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
            var player = GetFFAPlayer(parameters.player);
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

            var kPlayer = GetFFAPlayer(parameters.killer);
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
            var fPlayer = GetFFAPlayer(player);
            if (fPlayer == null)
            {
                return;
            }

            fPlayer.GamePlayer.OnRevived();
            SpawnPlayer(fPlayer, false);
        }

        public override void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition)
        {
            if (GetFFAPlayer(player.Player) == null)
            {
                return;
            }

            if (SpawnPoints.Count == 0)
            {
                return;
            }

            var spawnPoint = SpawnPoints.Count > 0 ? SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)] : UnavailableSpawnPoints[UnityEngine.Random.Range(0, UnavailableSpawnPoints.Count)];

            if (SpawnPoints.Count > 0)
            {
                Plugin.Instance.StartCoroutine(SpawnUsedUp(spawnPoint));
            }

            respawnPosition = spawnPoint.GetSpawnPoint();
            player.GiveSpawnProtection(Config.FFA.SpawnProtectionSeconds);
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
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(player.SteamID, out PlayerData data))
                {
                    return;
                }

                var iconLink = Plugin.Instance.UIManager.Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Plugin.Instance.UIManager.Icons.TryGetValue(0, out icon) ? icon.IconLink28 : "");
                var updatedText = $"<color={Config.FFA.ChatPlayerHexCode}>{player.Player.CharacterName.ToUnrich()}</color>: <color={Config.FFA.ChatMessageHexCode}>{text.ToUnrich()}</color>";

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
            R.Commands.Execute(player.GamePlayer.Player, $"/kit {Config.FFA.KitName}");
        }

        public void SpawnPlayer(FFAPlayer player, bool seperateSpawnPoint)
        {
            if (SpawnPoints.Count == 0)
            {
                return;
            }

            var spawnPoint = seperateSpawnPoint ? SpawnPoints[Players.IndexOf(player)] : (SpawnPoints.Count > 0 ? SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)] : UnavailableSpawnPoints[UnityEngine.Random.Range(0, UnavailableSpawnPoints.Count)]);
            if (!seperateSpawnPoint && SpawnPoints.Count > 0)
            {
                Plugin.Instance.StartCoroutine(SpawnUsedUp(spawnPoint));
            }
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), 0);
            player.GamePlayer.GiveSpawnProtection(Config.FFA.SpawnProtectionSeconds);
        }

        public override void PlayerChangeFiremode(GamePlayer player)
        {
            FFAPlayer fPlayer = GetFFAPlayer(player.Player);
            if (fPlayer == null)
            {
                return;
            }

            if (GamePhase == EGamePhase.Ending || GamePhase == EGamePhase.Starting)
            {
                return;
            }

            if (fPlayer.GamePlayer.HasScoreboard)
            {
                fPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideFFALeaderboard(fPlayer.GamePlayer);
            }
            else
            {
                fPlayer.GamePlayer.HasScoreboard = true;
                Plugin.Instance.UIManager.SetupFFALeaderboard(fPlayer, Players, Location, true);
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

        public IEnumerator SpawnUsedUp(FFASpawnPoint spawnPoint)
        {
            SpawnPoints.Remove(spawnPoint);
            UnavailableSpawnPoints.Add(spawnPoint);
            yield return new WaitForSeconds(Config.SpawnUnavailableSeconds);
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
