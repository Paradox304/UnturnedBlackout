using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnturnedLegends.Enums;
using UnturnedLegends.Models;
using UnturnedLegends.Structs;

namespace UnturnedLegends.GameTypes
{
    public abstract class Game
    {
        public EGameType GameType { get; set; }
        public Config Config { get; set; }
        public ArenaLocation CurrentLocation { get; set; }

        public bool HasStarted { get; set; }

        public Game(EGameType gameType, int locationID)
        {
            GameType = gameType;
            Config = Plugin.Instance.Configuration.Instance;
            CurrentLocation = Config.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
            HasStarted = false;

            PlayerLife.onPlayerDied += OnPlayerPostDeath;
        }

        private void OnPlayerPostDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            OnPlayerDead(sender.player, instigator);
        }

        public GamePlayer GetGamePlayer(UnturnedPlayer player)
        {
            return Plugin.Instance.GameManager.Players.TryGetValue(player.CSteamID, out GamePlayer gPlayer) ? gPlayer : null;
        }

        public GamePlayer GetGamePlayer(Player player)
        {
            return Plugin.Instance.GameManager.Players.TryGetValue(player.channel.owner.playerID.steamID, out GamePlayer gPlayer) ? gPlayer : null;
        }

        public GamePlayer GetGamePlayer(CSteamID steamID)
        {
            return Plugin.Instance.GameManager.Players.TryGetValue(steamID, out GamePlayer gPlayer) ? gPlayer : null;
        }

        public void Destroy()
        {
            PlayerLife.onPlayerDied -= OnPlayerPostDeath;
        }

        public abstract void OnPlayerDead(Player player, CSteamID killer);
        public abstract void AddPlayerToGame(GamePlayer player);
        public abstract void RemovePlayerFromGame(GamePlayer player);
    }
}
