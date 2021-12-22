using Rocket.Core;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.Enums;
using UnturnedLegends.Models;
using UnturnedLegends.SpawnPoints;
using UnturnedLegends.Structs;

namespace UnturnedLegends.GameTypes
{
    public class FFAGame : Game
    {
        public List<FFASpawnPoint> SpawnPoints { get; set; }
        public Dictionary<CSteamID, FFAPlayer> Players { get; set; }

        public FFAGame(int locationID) : base(EGameType.FFA, locationID)
        {
            Utility.Debug("Initializing FFA game");
            SpawnPoints = Plugin.Instance.DataManager.Data.FFASpawnPoints.Where(k => k.LocationID == locationID).ToList();
            Players = new Dictionary<CSteamID, FFAPlayer>();
            Utility.Debug($"Found {SpawnPoints.Count} positions for FFA");
        }

        public override void AddPlayerToGame(GamePlayer player)
        {
            Utility.Debug($"Adding {player.Player.CharacterName} to FFA game");
            if (Players.ContainsKey(player.SteamID))
            {
                Utility.Debug("Player is already in the game, returning");
                return;
            }

            FFAPlayer fPlayer = new FFAPlayer(player);
            Players.Add(player.SteamID, fPlayer);
            GiveLoadout(fPlayer);
            SpawnPlayer(fPlayer);
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            Utility.Debug($"Removing {player.Player.CharacterName} from FFA game");
            if (!Players.ContainsKey(player.SteamID))
            {
                Utility.Debug("Player is already not in the game, returning");
                return;
            }

            Players.Remove(player.SteamID);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb)
        {
            Utility.Debug($"Player died, getting the ffa player");
            var fPlayer = GetFFAPlayer(player);
            if (fPlayer == null)
            {
                Utility.Debug("Could'nt find the ffa player, returning");
                return;
            }

            Utility.Debug($"Game player found, player name: {fPlayer.GamePlayer.Player.CharacterName}");
            Utility.Debug("Respawning the player");
            fPlayer.OnDeath();
            player.life.sendRespawn(false);
            TaskDispatcher.QueueOnMainThread(() =>
            {
                SpawnPlayer(fPlayer);
            });

            var kPlayer = GetFFAPlayer(killer);
            if (kPlayer == null)
            {
                Utility.Debug("Could'nt find the killer, returning");
                return;
            }

            kPlayer.Kills++;

            var xpGained = 0;
            if (limb == ELimb.SKULL)
            {
                xpGained += Config.FFA.XPPerKillHeadshot;
            } else
            {
                xpGained += Config.FFA.XPPerKill;
            }

            if (kPlayer.KillStreak != 0)
            {
                kPlayer.KillStreak++;
                xpGained += Config.FFA.BaseXPKS + (kPlayer.KillStreak * Config.FFA.IncreaseXPPerKS);
            }

            if (kPlayer.MultipleKills == 0)
            {
                kPlayer.MultipleKills++;
            } else
            {
                if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
                {
                    kPlayer.MultipleKills++;
                    xpGained += Config.FFA.BaseXPMK + (kPlayer.MultipleKills * Config.FFA.IncreaseXPPerMK);
                } else
                {
                    kPlayer.MultipleKills = 0;
                }
            }
            kPlayer.LastKill = DateTime.UtcNow;

            de
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
        }

        public FFAPlayer GetFFAPlayer(CSteamID steamID)
        {
            return Players.TryGetValue(steamID, out FFAPlayer fPlayer) ? fPlayer : null;
        }

        public FFAPlayer GetFFAPlayer(UnturnedPlayer player)
        {
            return Players.TryGetValue(player.CSteamID, out FFAPlayer fPlayer) ? fPlayer : null;
        }

        public FFAPlayer GetFFAPlayer(Player player)
        {
            return Players.TryGetValue(player.channel.owner.playerID.steamID, out FFAPlayer fPlayer) ? fPlayer : null;
        }
    }
}
