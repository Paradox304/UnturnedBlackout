using Rocket.Core;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
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
                yield return new WaitForSeconds(1);
                // CODE TO SEND THE TIMER UI
            }
            HasStarted = true;

            foreach (var player in Players)
            {
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
                // CODE TO SEND THE WHOLE FFA UI
            }

            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            yield return new WaitForSeconds(Config.FFA.EndSeconds);
            foreach (var player in Players)
            {
                Plugin.Instance.GameManager.SendPlayerToLobby(player.GamePlayer.Player);

                // CODE TO SEND THE UI REMOVAL
            }
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

            // CODE TO SEND THE WHOLE FFA UI
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            Utility.Debug($"Removing {player.Player.CharacterName} from FFA game");
            if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already not in the game, returning");
                return;
            }

            // CODE TO SEND THE UI REMOVAL

            Players.RemoveAll(k => k.GamePlayer.SteamID == player.SteamID);
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

            // CODE TO UPDATE SHOW HOW MUCH XP PLAYER GAINED, ALSO CODE TO UPDATE THE KILLS FOR THE PLAYER AND TOP KILLS FOR ALL OTHER PLAYERS

            Utility.Debug($"Killer's killstreak: {kPlayer.KillStreak}, Killer's XP gained: {xpGained}, Killer's Multiple Kills: {kPlayer.MultipleKills}");
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DBManager.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, (uint)xpGained);
            });
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
