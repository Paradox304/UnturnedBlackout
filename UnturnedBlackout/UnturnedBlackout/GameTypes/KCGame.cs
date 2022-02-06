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
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models;

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
        public Coroutine SpawnSwitcher { get; set; }

        public KCGame(ArenaLocation location) : base(EGameType.KC, location)
        {
            Utility.Debug($"Initializing KC game for location {location.LocationName}");
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
            Utility.Debug($"Found {SpawnPoints.Count} spawnpoints registered");
            foreach (var key in SpawnPoints.Keys)
            {
                Utility.Debug(key.ToString());
            }
            Players = new List<KCPlayer>();
            PlayersLookup = new Dictionary<CSteamID, KCPlayer>();

            BlueTeam = new KCTeam(this, (byte)ETeam.Blue, false, Config.BlueDogTagID);
            RedTeam = new KCTeam(this, (byte)ETeam.Red, false, Config.RedDogTagID);

            GameStarter = Plugin.Instance.StartCoroutine(StartGame());
        }

        public IEnumerator StartGame()
        {
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

            SpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
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
                wonTeam = new KCTeam(this, -1, true, 0);
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
            for (int index = 0; index < Players.Count; index++)
            {
                var player = Players[index];
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
                Plugin.Instance.UIManager.SetupPreEndingUI(player.GamePlayer, EGameType.KC, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score);
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
            if (SpawnSwitcher != null)
            {
                Plugin.Instance.StopCoroutine(SpawnSwitcher);
            }

            StartVoting();
        }

        public override void AddPlayerToGame(GamePlayer player)
        {
            Utility.Debug($"Adding {player.Player.CharacterName} to KC game");
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already in the game, returning");
                return;
            }

            Utility.Debug($"Blue Players: {BlueTeam.Players.Count}, Red Players: {RedTeam.Players.Count}");
            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;

            Utility.Debug($"Found a team for player with id {team.TeamID}");
            KCPlayer kPlayer = new KCPlayer(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(kPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, kPlayer);
            GiveLoadout(kPlayer);

            if (GamePhase == EGamePhase.Starting)
            {
                player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UIManager.ShowCountdownUI(player);
                SpawnPlayer(kPlayer);
            }
            else
            {
                Plugin.Instance.UIManager.SendKCHUD(kPlayer, BlueTeam, RedTeam);
                SpawnPlayer(kPlayer);
            }
            Plugin.Instance.UIManager.SendPreEndingUI(kPlayer.GamePlayer);

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            Utility.Debug($"Removing {player.Player.CharacterName} from KC game");
            if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already not in the game, returning");
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
            Utility.Debug("Player died, getting the kc player");
            var vPlayer = GetKCPlayer(player);
            if (vPlayer == null)
            {
                Utility.Debug("Could'nt find the kc player, returning");
                return;
            }

            if (vPlayer.GamePlayer.HasScoreboard)
            {
                vPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideKCLeaderboard(vPlayer.GamePlayer);
            }

            var victimKS = vPlayer.KillStreak;
            Utility.Debug($"Game player found, player name: {vPlayer.GamePlayer.Player.CharacterName}");
            vPlayer.OnDeath(killer);
            vPlayer.GamePlayer.OnDeath(killer);
            vPlayer.Team.OnDeath(vPlayer.GamePlayer.SteamID);
            ItemManager.dropItem(new Item(vPlayer.Team.DogTagID, true), vPlayer.GamePlayer.Player.Player.transform.position, true, true, true);
            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(vPlayer.GamePlayer.SteamID, 1));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var kPlayer = GetKCPlayer(killer);
                if (kPlayer == null)
                {
                    Utility.Debug("Could'nt find the killer, returning");
                    return;
                }

                if (kPlayer.GamePlayer.SteamID == vPlayer.GamePlayer.SteamID)
                {
                    Utility.Debug("Player killed themselves, returning");
                    return;
                }

                Utility.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");
                if (vPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    vPlayer.GamePlayer.LastDamager.Pop();
                }

                if (vPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    var assister = GetKCPlayer(vPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        Utility.Debug($"Last damage done to the player by {assister.GamePlayer.Player.CharacterName}");
                        assister.Assists++;
                        assister.Score += Config.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UIManager.ShowXPUI(assister.GamePlayer, Config.KC.XPPerAssist, Plugin.Instance.Translate("Assist_Kill", vPlayer.GamePlayer.Player.CharacterName.ToUnrich()));
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

                if (kPlayer.KillStreak > 0)
                {
                    xpGained += Config.KC.BaseXPKS + (++kPlayer.KillStreak * Config.KC.IncreaseXPPerKS);
                }
                else
                {
                    kPlayer.KillStreak++;
                }

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
                OnKill(kPlayer.GamePlayer, vPlayer.GamePlayer, kPlayer.GamePlayer.Player.Player.equipment.itemID, kPlayer.Team == BlueTeam ? Config.BlueHexCode : Config.RedHexCode, vPlayer.Team == BlueTeam ? Config.BlueHexCode : Config.RedHexCode);
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
            var player = GetKCPlayer(parameters.player);
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

            var kPlayer = GetKCPlayer(parameters.killer);
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
            Utility.Debug("Player revived, getting the kc player");
            var kPlayer = GetKCPlayer(player);
            if (kPlayer == null)
            {
                Utility.Debug("Could'nt find the kc player, returning");
                return;
            }

            Utility.Debug($"Game player found, player name: {kPlayer.GamePlayer.Player.CharacterName}");
            Utility.Debug("Reviving the player");

            kPlayer.GamePlayer.OnRevived();
            SpawnPlayer(kPlayer);
        }

        public override void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            if (P == null) return;
            var kPlayer = GetKCPlayer(player.CSteamID);
            if (kPlayer == null) return;
            if (GamePhase == EGamePhase.Ending) return;
            Utility.Debug($"{player.CharacterName} picked up item with id {P.item.id}");
            var otherTeam = kPlayer.Team.TeamID == (byte)ETeam.Blue ? RedTeam : BlueTeam;
            
            if (kPlayer.Team.DogTagID == P.item.id)
            {
                Utility.Debug("Player denied kill");
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

            } else if (P.item.id == otherTeam.DogTagID)
            {
                Utility.Debug("Player confirmed kill");
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
            } else
            {
                return;
            }

            player.Player.inventory.removeItem((byte)inventoryGroup, inventoryIndex);
        }

        public void GiveLoadout(KCPlayer player)
        {
            Utility.Debug($"Giving loadout to {player.GamePlayer.Player.CharacterName}");

            player.GamePlayer.Player.Player.inventory.ClearInventory();
            R.Commands.Execute(player.GamePlayer.Player, $"/kit {((ETeam)player.Team.TeamID == ETeam.Blue ? Config.BlueKitName : Config.RedKitName)}");
        }

        public void SpawnPlayer(KCPlayer player)
        {
            Utility.Debug($"Spawning {player.GamePlayer.Player.CharacterName}, getting a random location");
            if (!SpawnPoints.TryGetValue(player.Team.SpawnPoint, out var spawnPoints))
            {
                Utility.Debug($"Could'nt find the spawnpoints for group {player.Team.SpawnPoint}");
                return;
            }

            if (spawnPoints.Count == 0)
            {
                Utility.Debug("No spawnpoints set for KC, returning");
                return;
            }

            var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), 0);
            player.GamePlayer.GiveSpawnProtection(Config.KC.SpawnProtectionSeconds);
        }

        public override void PlayerLeaned(PlayerAnimator obj)
        {
            if (obj.lean != 1) return;
            KCPlayer kPlayer = GetKCPlayer(obj.player);
            if (kPlayer == null) return;
            if (GamePhase == EGamePhase.Ending || GamePhase == EGamePhase.Starting) return;
            Utility.Debug($"{obj.player.channel.owner.playerID.characterName} leaned, lean {obj.lean}");

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
                    wonTeam = new KCTeam(this, -1, true, 0);
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
            Utility.Debug($"{kPlayer.GamePlayer.Player.CharacterName} changed stance to {obj.stance}");
            kPlayer.GamePlayer.OnStanceChanged(obj.stance);
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
            return Players.Exists(k => k.GamePlayer.SteamID == steamID);
        }

        public override int GetPlayerCount()
        {
            return Players.Count;
        }

        public override List<GamePlayer> GetPlayers()
        {
            return Players.Select(k => k.GamePlayer).ToList();
        }
    }
}

