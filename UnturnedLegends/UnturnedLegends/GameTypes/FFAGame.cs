using Rocket.Core;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnturnedLegends.Enums;
using UnturnedLegends.Models;
using UnturnedLegends.SpawnPoints;
using UnturnedLegends.Structs;

namespace UnturnedLegends.GameTypes
{
    public class FFAGame : Game
    {
        public List<FFASpawnPoint> SpawnPoints { get; set; }
        public List<FFAPlayer> Players { get; set; }

        public Coroutine GameStarter { get; set; }
        public Coroutine GameEnder { get; set; }

        public const ushort ID = 27620;
        public const short Key = 27620;

        public FFAGame(int locationID) : base(EGameType.FFA, locationID)
        {
            Utility.Debug("Initializing FFA game");
            SpawnPoints = Plugin.Instance.DataManager.Data.FFASpawnPoints.Where(k => k.LocationID == locationID).ToList();
            Players = new List<FFAPlayer>();
            Utility.Debug($"Found {SpawnPoints.Count} positions for FFA");

            GameStarter = Plugin.Instance.StartCoroutine(StartGame());
        }

        public IEnumerator StartGame()
        {
            for (int seconds = Config.FFA.StartSeconds; seconds >= 0; seconds--)
            {
                Utility.Debug($"Starting game in {seconds} seconds");
                yield return new WaitForSeconds(1);
                foreach (var player in Players)
                {
                    EffectManager.sendUIEffectText(Key, player.GamePlayer.TransportConnection, true, "CountdownNum", seconds.ToString());
                }
            }
            HasStarted = true;

            Utility.Debug("Starting game!");
            foreach (var player in Players)
            {
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);

                EffectManager.sendUIEffectVisibility(Key, player.GamePlayer.TransportConnection, true, "GamemodeFFA", true);
                EffectManager.sendUIEffectVisibility(Key, player.GamePlayer.TransportConnection, true, "StartCountdown", false);
                EffectManager.sendUIEffectVisibility(Key, player.GamePlayer.TransportConnection, true, "ScoreCounter", true);
            }

            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            Stopwatch watch = new Stopwatch();
            for (int seconds = Config.FFA.EndSeconds; seconds >= 0; seconds--)
            {
                Utility.Debug($"Ending game in {seconds} seconds");
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                watch.Start();
                foreach (var player in Players)
                {
                    EffectManager.sendUIEffectText(Key, player.GamePlayer.TransportConnection, true, "TimerTxt", timeSpan.ToString(@"m\:ss"));
                }
                watch.Stop();
                Utility.Debug($"Took {watch.ElapsedMilliseconds} ms to iterate through {Players.Count} players on sending the timer");
                watch.Reset();
            }

            foreach (var player in Players)
            {
                Plugin.Instance.GameManager.SendPlayerToLobby(player.GamePlayer.Player);
                EffectManager.askEffectClearByID(ID, player.GamePlayer.TransportConnection);
            }

            Plugin.Instance.GameManager.EndGame();
        }

        public override void AddPlayerToGame(GamePlayer player)
        {
            Utility.Debug($"Adding {player.Player.CharacterName} to FFA game");
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already in the game, returning");
                return;
            }

            FFAPlayer fPlayer = new FFAPlayer(player);

            Players.Add(fPlayer);
            GiveLoadout(fPlayer);
            SpawnPlayer(fPlayer);

            EffectManager.sendUIEffect(ID, Key, player.TransportConnection, true);
            if (!HasStarted)
            {
                EffectManager.sendUIEffectVisibility(Key, player.TransportConnection, true, "StartCountdown", true);
            } else
            {
                EffectManager.sendUIEffectVisibility(Key, player.TransportConnection, true, "ScoreCounter", true);
                EffectManager.sendUIEffectVisibility(Key, player.TransportConnection, true, "GamemodeFFA", true);
                ShowTopUI(fPlayer);
            }
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            Utility.Debug($"Removing {player.Player.CharacterName} from FFA game");
            if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already not in the game, returning");
                return;
            }

            EffectManager.askEffectClearByID(ID, player.TransportConnection);
            Players.RemoveAll(k => k.GamePlayer.SteamID == player.SteamID);

            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach (var ply in Players)
            {
                ShowTopUI(ply);
            }
            watch.Stop();
            Utility.Debug($"Took {watch.ElapsedMilliseconds} ms to iterate through {Players.Count} in sending the top UI");
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb)
        {
            Utility.Debug("Player died, getting the ffa player");
            var fPlayer = GetFFAPlayer(player);
            if (fPlayer == null)
            {
                Utility.Debug("Could'nt find the ffa player, returning");
                return;
            }

            Utility.Debug($"Game player found, player name: {fPlayer.GamePlayer.Player.CharacterName}");
            Utility.Debug("Respawning the player");
            fPlayer.OnDeath();
            TaskDispatcher.QueueOnMainThread(() =>
            {
                player.life.sendRespawn(false);
                SpawnPlayer(fPlayer);
            });

            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(fPlayer.GamePlayer.SteamID, 1));

            var kPlayer = GetFFAPlayer(killer);
            if (kPlayer == null)
            {
                Utility.Debug("Could'nt find the killer, returning");
                return;
            }

            if (kPlayer.GamePlayer.SteamID == fPlayer.GamePlayer.SteamID)
            {
                Utility.Debug("Player killed themselves, returning");
                return;
            }

            Utility.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");
            kPlayer.Kills++;

            var xpGained = limb == ELimb.SKULL ? Config.FFA.XPPerKillHeadshot : Config.FFA.XPPerKill;

            if (kPlayer.KillStreak > 0)
                xpGained += Config.FFA.BaseXPKS + (++kPlayer.KillStreak * Config.FFA.IncreaseXPPerKS);
            else 
                kPlayer.KillStreak++;

            if (kPlayer.MultipleKills == 0)
                kPlayer.MultipleKills++;
            else if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
                xpGained += Config.FFA.BaseXPMK + (++kPlayer.MultipleKills * Config.FFA.IncreaseXPPerMK);
            else
                kPlayer.MultipleKills = 0;

            kPlayer.LastKill = DateTime.UtcNow;
            kPlayer.XP += xpGained;

            Players.Sort((x, y) => x.Kills - y.Kills);
            Utility.Debug($"Killer's killstreak: {kPlayer.KillStreak}, Killer's XP gained: {xpGained}, Killer's Multiple Kills: {kPlayer.MultipleKills}");

            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach (var ply in Players)
            {
                ShowTopUI(ply);
            }
            watch.Stop();
            Utility.Debug($"Took {watch.ElapsedMilliseconds} to iterate through {Players.Count} players on sending the top UI");

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DBManager.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, (uint)xpGained);
            });
        }

        public void ShowTopUI(FFAPlayer player)
        {
            Utility.Debug($"Showing FFA top UI for {player.GamePlayer.Player.CharacterName}");
            if (Players.Count == 0)
            {
                Utility.Debug("There are no players registered in the game, returning");
                return;
            }

            var firstPlayer = Players[0];
            var secondPlayer = player;
            if (player.GamePlayer.SteamID == firstPlayer.GamePlayer.SteamID)
            {
                secondPlayer = Players.Count > 1 ? Players[2] : null;

                EffectManager.sendUIEffectVisibility(Key, player.GamePlayer.TransportConnection, true, "CounterWinning", true);
                EffectManager.sendUIEffectVisibility(Key, player.GamePlayer.TransportConnection, true, "CounterLosing", false);
            } else
            {
                EffectManager.sendUIEffectVisibility(Key, player.GamePlayer.TransportConnection, true, "CounterWinning", false);
                EffectManager.sendUIEffectVisibility(Key, player.GamePlayer.TransportConnection, true, "CounterLosing", true);
            }

            EffectManager.sendUIEffectText(Key, player.GamePlayer.TransportConnection, true, "1stPlacementName", firstPlayer.GamePlayer.Player.CharacterName);
            EffectManager.sendUIEffectText(Key, player.GamePlayer.TransportConnection, true, "1stPlacementScore", firstPlayer.Kills.ToString());

            EffectManager.sendUIEffectText(Key, player.GamePlayer.TransportConnection, true, "2ndPlacementPlace", secondPlayer != null ? Utility.GetOrdinal(Players.IndexOf(secondPlayer) + 1) : "0");
            EffectManager.sendUIEffectText(Key, player.GamePlayer.TransportConnection, true, "2ndPlacementName", secondPlayer != null ? secondPlayer.GamePlayer.Player.CharacterName : "NONE");
            EffectManager.sendUIEffectText(Key, player.GamePlayer.TransportConnection, true, "2ndPlacementScore", secondPlayer != null ? secondPlayer.Kills.ToString() : "0");
        }

        public void GiveLoadout(FFAPlayer player)
        {
            Utility.Debug($"Giving loadout to {player.GamePlayer.Player.CharacterName}");
            R.Commands.Execute(player.GamePlayer.Player, $"/kit {Config.KitName}");
        }

        public void SpawnPlayer(FFAPlayer player)
        {
            Utility.Debug($"Spawning {player.GamePlayer.Player.CharacterName}, getting a random location");
            if (SpawnPoints.Count == 0)
            {
                Utility.Debug("No spawnpoints set for FFA, returning");
                return;
            } 

            var spawnpoint = SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)];
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnpoint.GetSpawnPoint(), 0);

            if (!HasStarted)
            {
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(0);
            }
        }

        public FFAPlayer GetFFAPlayer(CSteamID steamID)
        {
            return Players.FirstOrDefault(k => k.GamePlayer.SteamID == steamID);
        }

        public FFAPlayer GetFFAPlayer(UnturnedPlayer player)
        {
            return Players.FirstOrDefault(k => k.GamePlayer.SteamID == player.CSteamID);
        }

        public FFAPlayer GetFFAPlayer(Player player)
        {
            return Players.FirstOrDefault(k => k.GamePlayer.SteamID == player.channel.owner.playerID.steamID);
        }
    }
}
