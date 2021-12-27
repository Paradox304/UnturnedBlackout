using Rocket.API;
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
using System.Threading;
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

        public FFAGame(ArenaLocation location, Arena arena, List<GamePlayer> players) : base(EGameType.FFA, location, arena)
        {
            Utility.Debug($"Initializing FFA game for arena ID {arena.ArenaID} and location {location.LocationName}");
            SpawnPoints = Plugin.Instance.DataManager.Data.FFASpawnPoints.Where(k => k.LocationID == location.LocationID).ToList();
            Players = new List<FFAPlayer>();
            Utility.Debug($"Found {SpawnPoints.Count} positions for FFA");
            Utility.Debug($"Found {players.Count} to preadd");
            foreach (var player in players)
            {
                AddPlayerToGame(player);
            }
            GameStarter = Plugin.Instance.StartCoroutine(StartGame());
        }

        public IEnumerator StartGame()
        {
            for (int seconds = Config.FFA.StartSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.SendCountdownSeconds(player.GamePlayer, seconds);
                }
            }
            HasStarted = true;

            foreach (var player in Players)
            {
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);

                Plugin.Instance.UIManager.ShowFFAHUD(player.GamePlayer);
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

            GameEnd();
        }

        public override void GameEnd()
        {
            // TO BE ADDED
            StartVoting(Players.Select(k => k.GamePlayer).ToList());
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


            Plugin.Instance.UIManager.ShowFFAHUD(player);
            if (!HasStarted)
            {
                Plugin.Instance.UIManager.ShowCountdownUI(player);
            }
            else
            {
                Plugin.Instance.UIManager.UpdateFFATopUI(fPlayer, Players);
            }

            if (Players.Count == 2)
            {
                foreach (var ply in Players)
                {
                    Plugin.Instance.UIManager.UpdateFFATopUI(ply, Players);
                }
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

            Plugin.Instance.UIManager.ClearFFAHUD(player);
            var fPlayer = GetFFAPlayer(player.Player);
            if (fPlayer != null)
            {
                fPlayer.Destroy();
                Players.Remove(fPlayer);
            }

            foreach (var ply in Players)
            {
                Plugin.Instance.UIManager.UpdateFFATopUI(ply, Players);
            }
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
            fPlayer.OnDeath();
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
            string xpText = limb == ELimb.SKULL ? Plugin.Instance.Translate("Headshot_Kill").ToRich() : Plugin.Instance.Translate("Normal_Kill").ToRich();
            xpText += "\n";

            if (kPlayer.KillStreak > 0)
            {
                xpGained += Config.FFA.BaseXPKS + (++kPlayer.KillStreak * Config.FFA.IncreaseXPPerKS);
                xpText += Plugin.Instance.Translate("KillStreak_Show", kPlayer.KillStreak).ToRich() + "\n";
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
                xpGained += Config.FFA.BaseXPMK + (++kPlayer.MultipleKills * Config.FFA.IncreaseXPPerMK);
                xpText += Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich() + "\n";
            }
            else
            {
                kPlayer.MultipleKills = 0;
            }

            kPlayer.LastKill = DateTime.UtcNow;


            Players.Sort((x, y) => y.Kills.CompareTo(x.Kills));
            Plugin.Instance.UIManager.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);

            foreach (var ply in Players)
            {
                Plugin.Instance.UIManager.UpdateFFATopUI(ply, Players);
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
            var player = GetFFAPlayer(parameters.player);
            if (player == null)
            {
                Utility.Debug("Player isn't ingame, returning");
                return;
            }

            if (!HasStarted)
            {
                shouldAllow = false;
                return;
            }

            if (player.HasSpawnProtection)
            {
                shouldAllow = false;
                return;
            }

            player.OnDamaged();

            var kPlayer = GetFFAPlayer(parameters.killer);
            if (kPlayer == null)
            {
                Utility.Debug("Killer not found, returning");
                return;
            }

            if (kPlayer.HasSpawnProtection)
            {
                kPlayer.HasSpawnProtection = false;
            }
        }

        public override void OnPlayerRevived(UnturnedPlayer player)
        {
            Utility.Debug("Player revived, getting the ffa player");
            var fPlayer = GetFFAPlayer(player);
            if (fPlayer == null)
            {
                Utility.Debug("Could'nt find the ffa player, returning");
                return;
            }

            Utility.Debug($"Game player found, player name: {fPlayer.GamePlayer.Player.CharacterName}");
            Utility.Debug("Reviving the player");

            var item = player.Player.inventory.getItem(0, 0);
            if (item != null)
            {
                player.Player.equipment.tryEquip(0, item.x, item.y);
            }

            SpawnPlayer(fPlayer);
        }

        public void GiveLoadout(FFAPlayer player)
        {
            Utility.Debug($"Giving loadout to {player.GamePlayer.Player.CharacterName}");

            player.GamePlayer.Player.Player.inventory.ClearInventory();
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

            player.GiveSpawnProtection();
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
