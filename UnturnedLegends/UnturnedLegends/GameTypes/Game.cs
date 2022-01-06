using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models;

namespace UnturnedBlackout.GameTypes
{
    public abstract class Game
    {
        public Config Config { get; set; }

        public EGameType GameMode { get; set; }
        public ArenaLocation Location { get; set; }

        public EGamePhase GamePhase { get; set; }

        public Dictionary<int, VoteChoice> VoteChoices { get; set; }
        public List<GamePlayer> Vote0 { get; set; }
        public List<GamePlayer> Vote1 { get; set; }

        public Coroutine VoteEnder { get; set; }

        public Game(EGameType gameMode, ArenaLocation location)
        {
            GameMode = gameMode;
            Config = Plugin.Instance.Configuration.Instance;
            Location = location;

            GamePhase = EGamePhase.Starting;

            VoteChoices = new Dictionary<int, VoteChoice>();
            Vote0 = new List<GamePlayer>();
            Vote1 = new List<GamePlayer>();

            PlayerLife.onPlayerDied += OnPlayerDied;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            DamageTool.damagePlayerRequested += OnPlayerDamaged;
        }

        public void StartVoting()
        {
            Utility.Debug($"Trying to start voting on location {Location.LocationName} with game mode {GameMode}");
            if (!Plugin.Instance.GameManager.CanStartVoting())
            {
                Utility.Debug("There is already a voting going on, returning");
                GamePhase = EGamePhase.WaitingForVoting;
                Plugin.Instance.UIManager.OnGameUpdated(this);
                return;
            }

            GamePhase = EGamePhase.Voting;
            Utility.Debug($"Starting voting on location {Location.LocationName} with game mode {GameMode}");

            var locations = Plugin.Instance.GameManager.AvailableLocations.ToList();
            locations.Add(Location.LocationID);
            var gameModes = new List<byte> { (byte)EGameType.FFA, (byte)EGameType.TDM };

            for (int i = 0; i <= 1; i++)
            {
                Utility.Debug($"Found {locations.Count} available locations to choose from");
                var locationID = locations[UnityEngine.Random.Range(0, locations.Count)];
                var location = Config.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
                Utility.Debug($"Found {gameModes.Count} available gamemodes to choose from");
                var gameMode = (EGameType)gameModes[UnityEngine.Random.Range(0, gameModes.Count)];
                Utility.Debug($"Found gamemode {gameMode}");
                var voteChoice = new VoteChoice(location, gameMode);
                gameModes.Remove((byte)gameMode);
                VoteChoices.Add(i, voteChoice);
            }

            VoteEnder = Plugin.Instance.StartCoroutine(EndVoter());
            Plugin.Instance.UIManager.OnGameUpdated(this);
        }

        public IEnumerator EndVoter()
        {
            for (int seconds = Config.VoteSeconds; seconds >= 0; seconds--)
            {
                TimeSpan span = TimeSpan.FromSeconds(seconds);
                Plugin.Instance.UIManager.OnGameVoteTimerUpdated(this, span.ToString(@"m\:ss"));
                yield return new WaitForSeconds(1);
            }

            Utility.Debug("Ending voting, getting the most voted choice");
            VoteChoice choice;
            if (Vote0.Count >= Vote1.Count)
            {
                choice = VoteChoices[0];
            }
            else
            {
                choice = VoteChoices[1];
            }
            Utility.Debug($"Most voted choice: {choice.Location.LocationName}, {choice.GameMode}");
            EndVoting(choice);
        }

        public void OnVoted(GamePlayer player, int choice)
        {
            Utility.Debug($"{player.Player.CharacterName} chose {choice}");
            if (Vote0.Contains(player))
            {
                if (choice == 0)
                {
                    return;
                }

                Vote0.Remove(player);
                Vote1.Add(player);
            }
            else if (Vote1.Contains(player))
            {
                if (choice == 1)
                {
                    return;
                }

                Vote1.Remove(player);
                Vote0.Add(player);
            }
            else
            {
                if (choice == 1)
                {
                    Vote1.Add(player);
                }
                else
                {
                    Vote0.Add(player);
                }
            }
            Plugin.Instance.UIManager.OnGameVoteCountUpdated(this);
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

        public abstract bool IsPlayerIngame(CSteamID steamID);
        public abstract void OnPlayerRevived(UnturnedPlayer player);
        public abstract void OnPlayerDead(Player player, CSteamID killer, ELimb limb);
        public abstract void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow);
        public abstract void AddPlayerToGame(GamePlayer player);
        public abstract void RemovePlayerFromGame(GamePlayer player);
        public abstract int GetPlayerCount();
    }
}
