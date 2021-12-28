using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnturnedLegends.Enums;
using UnturnedLegends.Models;
using UnturnedLegends.Structs;

namespace UnturnedLegends.GameTypes
{
    public abstract class Game
    {
        public Config Config { get; set; }

        public EGameType GameMode { get; set; }
        public ArenaLocation Location { get; set; }
        
        public EGamePhase GamePhase { get; set; }

        public Dictionary<int, VoteChoice> VoteChoices { get; set; }
        public List<GamePlayer> Vote1 { get; set; }
        public List<GamePlayer> Vote2 { get; set; }

        public Coroutine VoteEnder { get; set; }

        public Game(EGameType gameMode, ArenaLocation location)
        {
            GameMode = gameMode;
            Config = Plugin.Instance.Configuration.Instance;
            Location = location;

            GamePhase = EGamePhase.Starting;

            VoteChoices = new Dictionary<int, VoteChoice>();
            Vote1 = new List<GamePlayer>();
            Vote2 = new List<GamePlayer>();

            PlayerLife.onPlayerDied += OnPlayerDied;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            DamageTool.damagePlayerRequested += OnPlayerDamaged;
        }

        public void StartVoting()
        {
            if (!Plugin.Instance.GameManager.CanStartVoting())
            {
                GamePhase = EGamePhase.WaitingForVoting;
                return;
            }

            GamePhase = EGamePhase.Voting;
            Utility.Debug($"Starting voting on location {Location.LocationName} with game mode {GameMode}");

            var locations = Plugin.Instance.GameManager.AvailableLocations.ToList();
            locations.Add(Location.LocationID);
            var gameModes = new List<byte> { (byte)GameMode };

            for (int i = 1; i <= 2; i++)
            {
                Utility.Debug($"Found {locations.Count} available locations to choose from");
                var locationID = locations[UnityEngine.Random.Range(0, locations.Count)];
                var location = Config.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
                Utility.Debug($"Found {gameModes.Count} available gamemodes to choose from");
                var gameMode = (EGameType)gameModes[UnityEngine.Random.Range(0, gameModes.Count)];
                Utility.Debug($"Found gamemode {gameMode}");
                var voteChoice = new VoteChoice(location, gameMode);
                VoteChoices.Add(i, voteChoice);
            }

            VoteEnder = Plugin.Instance.StartCoroutine(EndVoter());
        }

        public IEnumerator EndVoter()
        {
            yield return new WaitForSeconds(Config.VoteSeconds);
            Utility.Debug("Ending voting, getting the most voted choice");
            VoteChoice choice = null;
            if (Vote1.Count >= Vote2.Count)
            {
                choice = VoteChoices[1];
            } else
            {
                choice = VoteChoices[2];
            }
            Utility.Debug($"Most voted choice: {choice.Location.LocationName}, {choice.GameMode}");
            EndVoting(choice);
        }

        public void OnVoted(GamePlayer player, int choice)
        {
            Utility.Debug($"{player.Player.CharacterName} chose {choice}");
            if (Vote1.Contains(player))
            {
                if (choice == 1) return;
                Vote1.Remove(player);
                Vote2.Add(player);
            } else if (Vote2.Contains(player))
            {
                if (choice == 2) return;
                Vote2.Remove(player);
                Vote1.Add(player);
            } else
            {
                if (choice == 1)
                {
                    Vote1.Add(player);
                } else
                {
                    Vote2.Add(player);
                }
            }
        }

        public void EndVoting(VoteChoice choice)
        {
            Utility.Debug($"Stopping voting for game");
            Utility.Debug($"Ending the current game and starting a new one at the location {choice.Location.LocationName}, gamemode {choice.GameMode}");

            GamePhase = EGamePhase.Ended;
            Plugin.Instance.GameManager.EndGame(this);
            Plugin.Instance.GameManager.StartGame(choice.Location, choice.GameMode);
            Plugin.Instance.GameManager.OnVotingEnded();
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
        public abstract bool IsPlayerIngame(CSteamID steamID);
        public abstract void OnPlayerRevived(UnturnedPlayer player);
        public abstract void OnPlayerDead(Player player, CSteamID killer, ELimb limb);
        public abstract void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow);
        public abstract void AddPlayerToGame(GamePlayer player);
        public abstract void RemovePlayerFromGame(GamePlayer player);
        public abstract int GetPlayerCount();
    }
}
