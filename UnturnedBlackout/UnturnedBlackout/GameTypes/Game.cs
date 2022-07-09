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
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Feed;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.GameTypes
{
    public abstract class Game
    {
        public ConfigManager Config
        {
            get
            {
                return Plugin.Instance.ConfigManager;
            }
        }

        public EGameType GameMode { get; set; }
        public ArenaLocation Location { get; set; }
        public bool IsHardcore { get; set; }

        public EGamePhase GamePhase { get; set; }

        public List<Feed> Killfeed { get; set; }

        public List<GamePlayer> PlayersTalking { get; set; }

        public Coroutine VoteEnder { get; set; }
        public Coroutine KillFeedChecker { get; set; }

        public Game(EGameType gameMode, ArenaLocation location, bool isHardcore)
        {
            GameMode = gameMode;
            Location = location;
            IsHardcore = isHardcore;
            GamePhase = EGamePhase.WaitingForPlayers;
            Killfeed = new List<Feed>();

            PlayersTalking = new List<GamePlayer>();

            UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            PlayerLife.OnSelectingRespawnPoint += OnPlayerRespawning;
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            UnturnedPlayerEvents.OnPlayerInventoryAdded += OnPlayerPickupItem;
            DamageTool.damagePlayerRequested += OnPlayerDamaged;
            ChatManager.onChatted += OnChatted;
            ItemManager.onTakeItemRequested += OnTakeItem;
            UseableThrowable.onThrowableSpawned += OnThrowableSpawned;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            UseableConsumeable.onConsumePerformed += OnConsumed;
            UseableGun.OnAimingChanged_Global += OnAimingChanged;

            KillFeedChecker = Plugin.Instance.StartCoroutine(UpdateKillfeed());
        }

        private void OnAimingChanged(UseableGun obj)
        {
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(obj.player);
            if (gPlayer == null)
            {
                return;
            }

            PlayerAimingChanged(gPlayer, obj.isAiming);
        }

        private void OnConsumed(Player instigatingPlayer, ItemConsumeableAsset consumeableAsset)
        {
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(instigatingPlayer);
            if (gPlayer == null)
            {
                return;
            }

            PlayerConsumeableUsed(gPlayer, consumeableAsset);
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            var steamID = new CSteamID(drop.GetServersideData()?.owner ?? 0UL);
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(steamID);
            if (gPlayer == null)
            {
                return;
            }

            PlayerBarricadeSpawned(gPlayer, drop);
        }

        private void OnThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(useable.player);
            if (gPlayer == null)
            {
                return;
            }

            PlayerThrowableSpawned(gPlayer, useable);
        }

        public void OnTrapTriggered(GamePlayer player, BarricadeDrop drop)
        {
            var trapOwner = Plugin.Instance.GameManager.GetGamePlayer(new CSteamID(drop.GetServersideData().owner));
            Logging.Debug($"Trap triggered by {player.Player.CharacterName}, owner is {(trapOwner?.Player?.CharacterName ?? "None")}");
            if (trapOwner != null)
            {
                Logging.Debug("Trap owner is not null, adding trap owner to the player's last damager");
                if (player.LastDamager.Count > 0 && player.LastDamager.Peek() == trapOwner.SteamID)
                {
                    return;
                }
                player.LastDamager.Push(trapOwner.SteamID);
            }
        }

        private void OnPlayerRespawning(PlayerLife sender, bool wantsToSpawnAtHome, ref Vector3 position, ref float yaw)
        {
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(sender.player);
            if (gPlayer == null)
            {
                return;
            }

            OnPlayerRespawn(gPlayer, ref position, ref yaw);
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
            if (!PlayersTalking.Contains(player))
            {
                PlayersTalking.Add(player);
                OnVoiceChatUpdated(player);
            }
        }

        public void OnStoppedTalking(GamePlayer player)
        {
            if (PlayersTalking.Contains(player))
            {
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
            Plugin.Instance.UIManager.UpdateVoiceChatUI(players, talkingPlayers);
        }

        public void OnStanceChanged(PlayerStance obj)
        {
            PlayerStanceChanged(obj);
        }

        public void OnChangeFiremode(GamePlayer player)
        {
            PlayerChangeFiremode(player);
        }

        private void OnPlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            PlayerPickupItem(player, inventoryGroup, inventoryIndex, P);
        }

        public void OnKill(GamePlayer killer, GamePlayer victim, ushort weaponID, string killerColor, string victimColor)
        {
            if (!Plugin.Instance.UIManager.KillFeedIcons.TryGetValue(weaponID, out FeedIcon icon))
            {
                return;
            }

            var feed = new Feed($"<color={killerColor}>{killer.Player.CharacterName.ToUnrich()}</color> {icon.Symbol} <color={victimColor}>{victim.Player.CharacterName.ToUnrich()}</color>", DateTime.UtcNow);
            if (Killfeed.Count < Config.Base.FileData.MaxKillFeed)
            {
                Killfeed.Add(feed);
                OnKillfeedUpdated();
                return;
            }

            var removeKillfeed = Killfeed.OrderBy(k => k.Time).FirstOrDefault();

            Killfeed.Remove(removeKillfeed);
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
                yield return new WaitForSeconds(Config.Base.FileData.KillFeedSeconds);
                if (Killfeed.RemoveAll(k => (DateTime.UtcNow - k.Time).TotalSeconds >= Config.Base.FileData.KillFeedSeconds) > 0)
                {
                    OnKillfeedUpdated();
                }
            }
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
            PlayerLife.OnSelectingRespawnPoint -= OnPlayerRespawning;
            UnturnedPlayerEvents.OnPlayerRevive -= OnPlayerRevive;
            UnturnedPlayerEvents.OnPlayerInventoryAdded -= OnPlayerPickupItem;
            DamageTool.damagePlayerRequested -= OnPlayerDamaged;
            ChatManager.onChatted -= OnChatted;
            ItemManager.onTakeItemRequested -= OnTakeItem;
            UseableThrowable.onThrowableSpawned -= OnThrowableSpawned;
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            UseableConsumeable.onConsumePerformed -= OnConsumed;
            UseableGun.OnAimingChanged_Global -= OnAimingChanged;

            if (KillFeedChecker != null)
            {
                Plugin.Instance.StopCoroutine(KillFeedChecker);
            }
        }

        public abstract bool IsPlayerIngame(CSteamID steamID);
        public abstract void OnPlayerRevived(UnturnedPlayer player);
        public abstract void OnPlayerRespawn(GamePlayer player, ref Vector3 respawnPosition, ref float yaw);
        public abstract void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause);
        public abstract void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow);
        public abstract IEnumerator AddPlayerToGame(GamePlayer player);
        public abstract void RemovePlayerFromGame(GamePlayer player);
        public abstract void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P);
        public abstract void PlayerChangeFiremode(GamePlayer player);
        public abstract void PlayerStanceChanged(PlayerStance obj);
        public abstract void PlayerThrowableSpawned(GamePlayer player, UseableThrowable throwable);
        public abstract void PlayerBarricadeSpawned(GamePlayer player, BarricadeDrop drop);
        public abstract void PlayerConsumeableUsed(GamePlayer player, ItemConsumeableAsset consumeableAsset);
        public abstract void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible);
        public abstract void OnVoiceChatUpdated(GamePlayer player);
        public abstract void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow);
        public abstract void PlayerEquipmentChanged(GamePlayer player);
        public abstract void PlayerAimingChanged(GamePlayer player, bool isAiming);
        public abstract int GetPlayerCount();
        public abstract bool IsPlayerCarryingFlag(GamePlayer player);
        public abstract List<GamePlayer> GetPlayers();
    }
}