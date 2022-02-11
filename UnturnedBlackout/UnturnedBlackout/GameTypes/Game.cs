using Rocket.Unturned.Enumerations;
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
using UnturnedBlackout.Models.Feed;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.GameTypes
{
    public abstract class Game
    {
        public Config Config { get; set; }

        public EGameType GameMode { get; set; }
        public ArenaLocation Location { get; set; }

        public EGamePhase GamePhase { get; set; }

        public List<Feed> Killfeed { get; set; }

        public Dictionary<int, VoteChoice> VoteChoices { get; set; }
        public List<GamePlayer> Vote0 { get; set; }
        public List<GamePlayer> Vote1 { get; set; }

        public List<GamePlayer> PlayersTalking { get; set; }

        public Coroutine VoteEnder { get; set; }
        public Coroutine KillFeedChecker { get; set; }

        public Game(EGameType gameMode, ArenaLocation location)
        {
            GameMode = gameMode;
            Config = Plugin.Instance.Configuration.Instance;
            Location = location;

            GamePhase = EGamePhase.Starting;
            Killfeed = new List<Feed>();

            VoteChoices = new Dictionary<int, VoteChoice>();
            Vote0 = new List<GamePlayer>();
            Vote1 = new List<GamePlayer>();

            PlayersTalking = new List<GamePlayer>();

            UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            UnturnedPlayerEvents.OnPlayerInventoryAdded += OnPlayerPickupItem;
            PlayerAnimator.OnLeanChanged_Global += OnLeaned;
            DamageTool.damagePlayerRequested += OnPlayerDamaged;
            ChatManager.onChatted += OnChatted;
            ItemManager.onTakeItemRequested += OnTakeItem;

            KillFeedChecker = Plugin.Instance.StartCoroutine(UpdateKillfeed());
        }

        private void OnTakeItem(Player player, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, ItemData itemData, ref bool shouldAllow)
        {
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(player);
            if (gPlayer == null)
            {
                shouldAllow = false;
                return;
            }

            OnTakingItem(gPlayer, itemData, ref shouldAllow);
        }

        private void OnChatted(SteamPlayer player, EChatMode mode, ref Color chatted, ref bool isRich, string text, ref bool isVisible)
        {
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(player.player);
            if (gPlayer == null)
            {
                isVisible = false;
                return;
            }
            OnChatMessageSent(gPlayer, mode, text, ref isVisible);
        }

        public void OnStartedTalking(GamePlayer player)
        {
            Utility.Debug($"{player.Player.CharacterName} started talking");
            if (!PlayersTalking.Contains(player))
            {
                Utility.Debug("Add him to the list and check");
                PlayersTalking.Add(player);
                OnVoiceChatUpdated(player);
            }
        }

        public void OnStoppedTalking(GamePlayer player)
        {
            Utility.Debug($"{player.Player.CharacterName} stopped talking");
            if (PlayersTalking.Contains(player))
            {
                Utility.Debug("Remove him from the list and check");
                PlayersTalking.Remove(player);
                OnVoiceChatUpdated(player);
            }
        }

        public void SendVoiceChat(List<GamePlayer> players, bool isTeam)
        {
            var talkingPlayers = PlayersTalking;
            if (isTeam)
            {
                talkingPlayers = PlayersTalking.Where(k => players.Contains(k)).ToList();
            }
            Plugin.Instance.UIManager.SendVoiceChat(players, GameMode, GamePhase == EGamePhase.Ending, talkingPlayers);
        }

        public void OnStanceChanged(PlayerStance obj)
        {
            PlayerStanceChanged(obj);
        }

        private void OnLeaned(PlayerAnimator obj)
        {
            PlayerLeaned(obj);
        }

        private void OnPlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            PlayerPickupItem(player, inventoryGroup, inventoryIndex, P);
        }

        public void StartVoting()
        {
            Utility.Debug($"Trying to start voting on location {Location.LocationName} with game mode {GameMode}");
            if (KillFeedChecker != null)
            {
                Plugin.Instance.StopCoroutine(KillFeedChecker);
            }
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
            var gameModes = new List<byte> { (byte)EGameType.FFA, (byte)EGameType.TDM, (byte)EGameType.KC };

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

        public void OnKill(GamePlayer killer, GamePlayer victim, ushort weaponID, string killerColor, string victimColor)
        {
            Utility.Debug($"Adding killfeed for killer {killer.Player.CharacterName} victim {victim.Player.CharacterName} with weapon id {weaponID}");
            if (!Plugin.Instance.UIManager.KillFeedIcons.TryGetValue(weaponID, out FeedIcon icon))
            {
                Utility.Debug("Icon not found, return");
                return;
            }

            var feed = new Feed($"<color={killerColor}>{killer.Player.CharacterName.ToUnrich().Trim()}</color> {icon.Symbol} <color={victimColor}>{victim.Player.CharacterName.ToUnrich().Trim()}</color>", DateTime.UtcNow);
            if (Killfeed.Count < Config.MaxKillFeed)
            {
                Utility.Debug($"{Killfeed.Count} is less than {Config.MaxKillFeed}, add");
                Killfeed.Add(feed);
                OnKillfeedUpdated();
                return;
            }

            Utility.Debug($"Killfeed is not less than {Config.MaxKillFeed} find the oldest one");
            var removeKillfeed = Killfeed.OrderBy(k => k.Time).FirstOrDefault();
            Utility.Debug($"Found the oldest one with date {removeKillfeed.Time}");
            Killfeed.Remove(removeKillfeed);
            Utility.Debug("Removed the old killfeed, and add the new one");
            Killfeed.Add(feed);
            OnKillfeedUpdated();
        }

        public void OnKillfeedUpdated()
        {
            Plugin.Instance.UIManager.SendKillfeed(GetPlayers(), GameMode, Killfeed);
        }

        public IEnumerator UpdateKillfeed()
        {
            while (true)
            {
                yield return new WaitForSeconds(Config.KillFeedSeconds);
                if (Killfeed.RemoveAll(k => (DateTime.UtcNow - k.Time).TotalSeconds >= Config.KillFeedSeconds) > 0)
                {
                    Utility.Debug("Updating killfeed");
                    OnKillfeedUpdated();
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

        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            OnPlayerDead(player.Player, murderer, limb, cause);
        }

        public void WipeItems()
        {
            foreach (var region in ItemManager.regions)
            {
                region.items.RemoveAll(k => LevelNavigation.tryGetNavigation(k.point, out byte nav) && nav == Location.NavMesh);
            }
        }

        public void Destroy()
        {
            UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerRevive -= OnPlayerRevive;
            UnturnedPlayerEvents.OnPlayerInventoryAdded -= OnPlayerPickupItem;
            DamageTool.damagePlayerRequested -= OnPlayerDamaged;
            PlayerAnimator.OnLeanChanged_Global -= OnLeaned;
            ChatManager.onChatted -= OnChatted;
        }

        public abstract bool IsPlayerIngame(CSteamID steamID);
        public abstract void OnPlayerRevived(UnturnedPlayer player);
        public abstract void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause);
        public abstract void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow);
        public abstract void AddPlayerToGame(GamePlayer player);
        public abstract void RemovePlayerFromGame(GamePlayer player);
        public abstract void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P);
        public abstract void PlayerLeaned(PlayerAnimator obj);
        public abstract void PlayerStanceChanged(PlayerStance obj);
        public abstract void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible);
        public abstract void OnVoiceChatUpdated(GamePlayer player);
        public abstract void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow);
        public abstract int GetPlayerCount();
        public abstract List<GamePlayer> GetPlayers();
    }
}
