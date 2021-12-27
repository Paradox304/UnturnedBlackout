using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Linq;
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

        public ArenaLocation Location { get; set; }
        public Arena Arena { get; set; }

        public bool IsVoting { get; set; }
        public bool HasStarted { get; set; }

        public Game(EGameType gameType, int locationID, int arenaID)
        {
            GameType = gameType;
            Config = Plugin.Instance.Configuration.Instance;
            Location = Config.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
            Arena = Config.Arenas.FirstOrDefault(k => k.ArenaID == arenaID);

            IsVoting = false;
            HasStarted = false;

            PlayerLife.onPlayerDied += OnPlayerDied;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            DamageTool.damagePlayerRequested += OnPlayerDamaged;
        }

        private void OnPlayerDamaged(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            OnPlayerDamage(ref parameters, ref shouldAllow);
        }

        private void OnPlayerRevive(UnturnedPlayer player, Vector3 position, byte angle)
        {
            OnPlayerRevived(player);
        }

        private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            OnPlayerDead(sender.player, instigator, limb);
        }

        public void Destroy()
        {
            PlayerLife.onPlayerDied -= OnPlayerDied;
            UnturnedPlayerEvents.OnPlayerRevive -= OnPlayerRevive;
            DamageTool.damagePlayerRequested -= OnPlayerDamaged;
        }

        public abstract void GameEnd();
        public abstract bool IsPlayerIngame(Player player);
        public abstract bool IsPlayerIngame(CSteamID steamID);
        public abstract bool IsPlayerIngame(UnturnedPlayer player);
        public abstract bool IsPlayerIngame(GamePlayer player);
        public abstract void OnPlayerRevived(UnturnedPlayer player);
        public abstract void OnPlayerDead(Player player, CSteamID killer, ELimb limb);
        public abstract void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow);
        public abstract void AddPlayerToGame(GamePlayer player);
        public abstract void RemovePlayerFromGame(GamePlayer player);
    }
}
