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

namespace UnturnedLegends.GameTypes
{
    public abstract class Game
    {
        public EGameType GameType { get; set; }

        public Game(EGameType gameType)
        {
            GameType = gameType;

            PlayerLife.OnPreDeath += OnPlayerPreDeath;
            PlayerLife.onPlayerDied += OnPlayerPostDeath;
        }

        public void Destroy()
        {
            PlayerLife.OnPreDeath -= OnPlayerPreDeath;
            PlayerLife.onPlayerDied -= OnPlayerPostDeath;
        }

        private void OnPlayerPreDeath(PlayerLife obj)
        {
            OnPlayerDying(obj.player);
        }

        private void OnPlayerPostDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            OnPlayerDead(sender.player, instigator);
        }

        public abstract void OnPlayerDying(Player player);
        public abstract void OnPlayerDead(Player player, CSteamID killer);
        public abstract void AddPlayerToGame(GamePlayer player);
        public abstract void RemovePlayerFromGame(GamePlayer player);
    }
}
