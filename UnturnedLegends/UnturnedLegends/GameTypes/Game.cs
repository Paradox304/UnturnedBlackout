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
            OnPlayerDead(sender.player, instigator, limb);
        }

        public void Destroy()
        {
            PlayerLife.onPlayerDied -= OnPlayerPostDeath;
        }

        public abstract void OnPlayerDead(Player player, CSteamID killer, ELimb limb);
        public abstract void AddPlayerToGame(GamePlayer player);
        public abstract void RemovePlayerFromGame(GamePlayer player);
    }
}
