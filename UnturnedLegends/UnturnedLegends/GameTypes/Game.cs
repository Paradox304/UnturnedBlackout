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
        public Arena Arena { get; set; }

        public bool IsVoting { get; set; }
        public bool HasStarted { get; set; }

        public List<VoteChoice> VoteChoices { get; set; }
        public List<GamePlayer> VotePlayers { get; set; }

        public Coroutine VoteEnder { get; set; }

        public Game(EGameType gameMode, ArenaLocation location, Arena arena)
        {
            GameMode = gameMode;
            Config = Plugin.Instance.Configuration.Instance;
            Location = location;
            Arena = arena;

            IsVoting = false;
            HasStarted = false;

            VoteChoices = new List<VoteChoice>();
            VotePlayers = new List<GamePlayer>();

            PlayerLife.onPlayerDied += OnPlayerDied;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            DamageTool.damagePlayerRequested += OnPlayerDamaged;
        }

        public void StartVoting(List<GamePlayer> players)
        {
            IsVoting = true;
            VotePlayers = players;
            Utility.Debug($"Starting voting on arena with id {Arena.ArenaID} and location {Location.LocationName} with game mode {GameMode}");
            for (int i = 1; i <= Config.VoteChoices; i++)
            {
                Utility.Debug("Getting the available locations to choose from");
                var locations = Arena.Locations.Where(k => Plugin.Instance.GameManager.AvailableLocations.Contains(k)).ToList();
                // Remove this when there are more locations available
                locations.Add(Location.LocationID);
                if (locations.Count == 0)
                {
                    break;
                }

                Utility.Debug($"Found {locations.Count} available locations to choose from");
                var locationID = locations[UnityEngine.Random.Range(0, locations.Count)];
                var location = Config.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);

                Utility.Debug("Getting the available gamemodes to choose from");
                // Remove this when there are more gamemodes available
                var gameModes = new List<byte> { (byte)GameMode };
                Utility.Debug($"Found {gameModes.Count} available gamemodes to choose from");
                var gameMode = (EGameType)gameModes[UnityEngine.Random.Range(0, gameModes.Count)];
                Utility.Debug($"Found gamemode {gameMode}");
                var voteChoice = new VoteChoice(location, gameMode);
                VoteChoices.Add(voteChoice);
            }

            if (VotePlayers.Count == 0 || VoteChoices.Count == 1)
            {
                Utility.Debug("There is only one vote choice or no players, choosing the first choice and sending it");
                EndVoting(VoteChoices[0]);
                return;
            }

            foreach (var player in VotePlayers)
            {
                // SEND CODE TO SHOW THE VOTING UI
            }

            VoteEnder = Plugin.Instance.StartCoroutine(EndVoter());
        }

        public IEnumerator EndVoter()
        {
            yield return new WaitForSeconds(Config.VoteSeconds);
            Utility.Debug("Ending voting, getting the most voted choice");
            int choice = (from i in (from x in VotePlayers
                                     select x.VoteChoice).ToList()
                          group i by i into grp
                          orderby grp.Count() descending
                          select grp.Key).FirstOrDefault();
            Utility.Debug($"Most voted choice: {choice}");
            if (choice == -1)
            {
                Utility.Debug("Choice is -1, sending the first choice");
                EndVoting(VoteChoices[0]);
            } else
            {
                EndVoting(VoteChoices[choice]);
            }
        }

        public void OnVoted(GamePlayer player, int choice)
        {
            Utility.Debug($"{player.Player.CharacterName} chose vote choice {choice}");
            if (choice > (VoteChoices.Count - 1))
            {
                Utility.Debug($"Choice is higher than the listed vote choices, returning");
                return;
            }

            player.VoteChoice = choice;
        }

        public void EndVoting(VoteChoice choice)
        {
            Utility.Debug($"Stopping voting for arena id {Arena.ArenaID}");
            Utility.Debug($"Ending the current game and starting a new one at the location {choice.Location.LocationName}, gamemode {choice.GameMode}, player count {VotePlayers.Count}");

            IsVoting = false;
            Plugin.Instance.GameManager.EndGame(this);
            Plugin.Instance.GameManager.StartGame(Arena, choice.Location, choice.GameMode, VotePlayers);
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
