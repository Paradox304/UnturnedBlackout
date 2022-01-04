using Rocket.Core;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnturnedLegends.Enums;
using UnturnedLegends.Models;

namespace UnturnedLegends.GameTypes
{
    public class TDMGame : Game
    {
        public List<TDMSpawnPoint> BlueSpawnPoints { get; set; }
        public List<TDMSpawnPoint> BlueUnavailableSpawnPoints { get; set; }

        public List<TDMSpawnPoint> RedSpawnPoints { get; set; }
        public List<TDMSpawnPoint> RedUnavailableSpawnPoints { get; set; }

        public List<TDMPlayer> Players { get; set; }

        public Team BlueTeam { get; set; }
        public Team RedTeam { get; set; }

        public Coroutine GameStarter { get; set; }
        public Coroutine GameEnder { get; set; }

        public TDMGame(ArenaLocation location) : base(EGameType.TDM, location)
        {
            Utility.Debug($"Initializing TDM game for location {location.LocationName}");
            BlueSpawnPoints = Plugin.Instance.DataManager.Data.TDMSpawnPoints.Where(k => k.LocationID == location.LocationID && k.TeamID == (byte)ETeam.Blue).ToList();
            RedSpawnPoints = Plugin.Instance.DataManager.Data.TDMSpawnPoints.Where(k => k.LocationID == location.LocationID && k.TeamID == (byte)ETeam.Red).ToList();
            Players = new List<TDMPlayer>();

            BlueUnavailableSpawnPoints = new List<TDMSpawnPoint>();
            RedUnavailableSpawnPoints = new List<TDMSpawnPoint>();
            Utility.Debug($"Found {BlueSpawnPoints.Count} positions for TDM for Blue Team");
            Utility.Debug($"Found {RedSpawnPoints.Count} positions for TDM for Red Team");

            BlueTeam = new Team((byte)ETeam.Blue, false);
            RedTeam = new Team((byte)ETeam.Red, false);

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

            Team wonTeam;
            if (BlueTeam.Score > RedTeam.Score)
            {
                wonTeam = BlueTeam;
            } else if (RedTeam.Score > BlueTeam.Score)
            {
                wonTeam = RedTeam;
            } else
            {
                wonTeam = new Team(-1, true);
            }
            Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
        }

        public IEnumerator GameEnd(Team wonTeam)
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
                Plugin.Instance.UIManager.SendPreEndingUI(player.GamePlayer, EGameType.TDM, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score);
            }
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UIManager.SetupTDMEndingLeaderboard(Players, Location, wonTeam));
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ShowTDMEndingLeaderboard(player.GamePlayer);
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

            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;

            Utility.Debug($"Found a team for player with id {team}");
            TDMPlayer tPlayer = new TDMPlayer(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(tPlayer);
            GiveLoadout(tPlayer);

            if (GamePhase == EGamePhase.Starting)
            {
                player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UIManager.ShowCountdownUI(player);
                SpawnPlayer(tPlayer, true);
            }
            else
            {
                Plugin.Instance.UIManager.SendTDMHUD(tPlayer, BlueTeam, RedTeam);
                SpawnPlayer(tPlayer, false);
            }

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

            Plugin.Instance.UIManager.ClearTDMHUD(player);
            var tPlayer = GetTDMPlayer(player.Player);

            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UIManager.ClearCountdownUI(player);
                tPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }
            else if (GamePhase == EGamePhase.Ending)
            {
                Plugin.Instance.UIManager.ClearPreEndingUI(player);
            }

            if (tPlayer != null)
            {
                tPlayer.GamePlayer.OnGameLeft();
                Players.Remove(tPlayer);
            }

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb)
        {
            Utility.Debug("Player died, getting the ffa player");
            var tPlayer = GetTDMPlayer(player);
            if (tPlayer == null)
            {
                Utility.Debug("Could'nt find the ffa player, returning");
                return;
            }
            var victimKS = tPlayer.KillStreak;

            Utility.Debug($"Game player found, player name: {tPlayer.GamePlayer.Player.CharacterName}");
            tPlayer.OnDeath();
            tPlayer.GamePlayer.OnDeath(killer);
            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(tPlayer.GamePlayer.SteamID, 1));

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
            kPlayer.Kills++;
            kPlayer.Team.Score++;
            kPlayer.Score += Config.KillPoints;

            var xpGained = limb == ELimb.SKULL ? Config.TDM.XPPerKillHeadshot : Config.TDM.XPPerKill;
            string xpText = limb == ELimb.SKULL ? Plugin.Instance.Translate("Headshot_Kill").ToRich() : Plugin.Instance.Translate("Normal_Kill").ToRich();
            xpText += "\n";

            if (kPlayer.KillStreak > 0)
            {
                xpGained += Config.TDM.BaseXPKS + (++kPlayer.KillStreak * Config.TDM.IncreaseXPPerKS);
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
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DBManager.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, (uint)xpGained);
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

            player.GamePlayer.OnDamaged();

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
            Utility.Debug("Player revived, getting the ffa player");
            var tPlayer = GetTDMPlayer(player);
            if (tPlayer == null)
            {
                Utility.Debug("Could'nt find the ffa player, returning");
                return;
            }

            Utility.Debug($"Game player found, player name: {tPlayer.GamePlayer.Player.CharacterName}");
            Utility.Debug("Reviving the player");

            tPlayer.GamePlayer.OnRevived();
            SpawnPlayer(tPlayer, false);
        }

        public void GiveLoadout(TDMPlayer player)
        {
            Utility.Debug($"Giving loadout to {player.GamePlayer.Player.CharacterName}");

            player.GamePlayer.Player.Player.inventory.ClearInventory();
            R.Commands.Execute(player.GamePlayer.Player, $"/kit {Config.KitName}");
        }

        public void SpawnPlayer(TDMPlayer player, bool seperateSpawnPoint)
        {
            Utility.Debug($"Spawning {player.GamePlayer.Player.CharacterName}, getting a random location");
            var spawnPoints = player.Team.TeamID == (byte)ETeam.Blue ? BlueSpawnPoints : RedSpawnPoints;
            var unavailableSpawnPoints = player.Team.TeamID == (byte)ETeam.Blue ? BlueUnavailableSpawnPoints : RedUnavailableSpawnPoints;
            if (spawnPoints.Count == 0)
            {
                Utility.Debug("No spawnpoints set for TDM, returning");
                return;
            }

            var spawnPoint = seperateSpawnPoint ? spawnPoints[Players.IndexOf(player)] : (spawnPoints.Count > 0 ? spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)] : unavailableSpawnPoints[UnityEngine.Random.Range(0, unavailableSpawnPoints.Count)]);
            if (!seperateSpawnPoint && spawnPoints.Count > 0)
            {
                Plugin.Instance.StartCoroutine(SpawnUsedUp(spawnPoint, (ETeam)player.Team.TeamID));
            }
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), 0);
            player.GamePlayer.GiveSpawnProtection(Config.TDM.SpawnProtectionSeconds);
        }

        public IEnumerator SpawnUsedUp(TDMSpawnPoint spawnPoint, ETeam team)
        {
            if (team == ETeam.Blue)
            {
                BlueSpawnPoints.Remove(spawnPoint);
                BlueUnavailableSpawnPoints.Add(spawnPoint);
                yield return new WaitForSeconds(Config.SpawnUnavailableSeconds);
                BlueSpawnPoints.Add(spawnPoint);
                BlueUnavailableSpawnPoints.Remove(spawnPoint);
            }
            else
            {

                RedSpawnPoints.Remove(spawnPoint);
                RedUnavailableSpawnPoints.Add(spawnPoint);
                yield return new WaitForSeconds(Config.SpawnUnavailableSeconds);
                RedSpawnPoints.Add(spawnPoint);
                RedUnavailableSpawnPoints.Remove(spawnPoint);
            }
        }

        public TDMPlayer GetTDMPlayer(CSteamID steamID)
        {
            return Players.FirstOrDefault(k => k.GamePlayer.SteamID == steamID);
        }

        public TDMPlayer GetTDMPlayer(UnturnedPlayer player)
        {
            return Players.FirstOrDefault(k => k.GamePlayer.SteamID == player.CSteamID);
        }

        public TDMPlayer GetTDMPlayer(Player player)
        {
            return Players.FirstOrDefault(k => k.GamePlayer.SteamID == player.channel.owner.playerID.steamID);
        }

        public override bool IsPlayerIngame(CSteamID steamID)
        {
            return Players.Exists(k => k.GamePlayer.SteamID == steamID);
        }

        public override int GetPlayerCount()
        {
            return Players.Count;
        }
    }
}
