using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using UnturnedBlackout.Database.Base;
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
                return Plugin.Instance.Config;
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
            GamePlayer gPlayer = Plugin.Instance.Game.GetGamePlayer(obj.player);
            if (gPlayer == null)
            {
                return;
            }

            // PlayerAimingChanged(gPlayer, obj.isAiming);
        }

        private void OnConsumed(Player instigatingPlayer, ItemConsumeableAsset consumeableAsset)
        {
            GamePlayer gPlayer = Plugin.Instance.Game.GetGamePlayer(instigatingPlayer);
            if (gPlayer == null)
            {
                return;
            }

            PlayerConsumeableUsed(gPlayer, consumeableAsset);
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            CSteamID steamID = new(drop.GetServersideData()?.owner ?? 0UL);
            GamePlayer gPlayer = Plugin.Instance.Game.GetGamePlayer(steamID);
            if (gPlayer == null)
            {
                return;
            }

            PlayerBarricadeSpawned(gPlayer, drop);
        }

        private void OnThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            GamePlayer gPlayer = Plugin.Instance.Game.GetGamePlayer(useable.player);
            if (gPlayer == null)
            {
                return;
            }

            PlayerThrowableSpawned(gPlayer, useable);
        }

        public void OnTrapTriggered(GamePlayer player, BarricadeDrop drop)
        {
            GamePlayer trapOwner = Plugin.Instance.Game.GetGamePlayer(new CSteamID(drop.GetServersideData().owner));
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
            GamePlayer gPlayer = Plugin.Instance.Game.GetGamePlayer(sender.player);
            if (gPlayer == null)
            {
                return;
            }

            OnPlayerRespawn(gPlayer, ref position, ref yaw);
        }

        private void OnTakeItem(Player player, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, ItemData itemData, ref bool shouldAllow)
        {
            GamePlayer gPlayer = Plugin.Instance.Game.GetGamePlayer(player);
            if (gPlayer == null)
            {
                shouldAllow = false;
                return;
            }

            OnTakingItem(gPlayer, itemData, ref shouldAllow);
        }

        private void OnChatted(SteamPlayer player, EChatMode mode, ref Color chatted, ref bool isRich, string text, ref bool isVisible)
        {
            GamePlayer gPlayer = Plugin.Instance.Game.GetGamePlayer(player.player);
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
            List<GamePlayer> talkingPlayers = PlayersTalking;
            if (isTeam)
            {
                talkingPlayers = PlayersTalking.Where(k => players.Contains(k)).ToList();
            }
            Plugin.Instance.UI.UpdateVoiceChatUI(players, talkingPlayers);
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
            if (!Plugin.Instance.UI.KillFeedIcons.TryGetValue(weaponID, out FeedIcon icon) && weaponID != 0 && weaponID != 1)
            {
                return;
            }

            Feed feed;
            if (weaponID == 0 || weaponID == 1)
            {
                feed = new Feed($"{(victim.Data.HasPrime ? UIManager.PRIME_SYMBOL : "")}<color={victimColor}>{victim.Player.CharacterName.ToUnrich()}</color> {(weaponID == 0 ? "" : "")} ", DateTime.UtcNow);
            }
            else
            {
                feed = new Feed($"{(killer.Data.HasPrime ? UIManager.PRIME_SYMBOL : "")}<color={killerColor}>{killer.Player.CharacterName.ToUnrich()}</color> {icon.Symbol} {(victim.Data.HasPrime ? UIManager.PRIME_SYMBOL : "")}<color={victimColor}>{victim.Player.CharacterName.ToUnrich()}</color>", DateTime.UtcNow);
            }

            if (Killfeed.Count < Config.Base.FileData.MaxKillFeed)
            {
                Killfeed.Add(feed);
                OnKillfeedUpdated();
                return;
            }

            Feed removeKillfeed = Killfeed.OrderBy(k => k.Time).FirstOrDefault();

            Killfeed.Remove(removeKillfeed);
            Killfeed.Add(feed);
            OnKillfeedUpdated();
        }

        public void OnKillfeedUpdated()
        {
            Plugin.Instance.UI.SendKillfeed(GetPlayers(), GameMode, Killfeed);
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

        public Case GetRandomRoundEndCase()
        {
            List<RoundEndCase> cases = Config.RoundEndCases.FileData.RoundEndCases;
            if (cases.Count == 0)
            {
                return null;
            }
            int poolSize = 0;
            foreach (RoundEndCase roundEndCase in cases) poolSize += roundEndCase.Weight;
            int randInt = UnityEngine.Random.Range(0, poolSize) + 1;

            int accumulatedProbability = 0;
            Case @case;
            for (int i = 0; i < cases.Count; i++)
            {
                RoundEndCase roundEndCase = cases[i];
                accumulatedProbability += roundEndCase.Weight;
                if (randInt <= accumulatedProbability)
                {
                    if (Plugin.Instance.DB.Cases.TryGetValue(roundEndCase.CaseID, out @case))
                    {
                        return @case;
                    }
                    Logging.Debug($"Case with id {roundEndCase.CaseID} not found for selecting round end case");
                    break;
                }
            }
            return Plugin.Instance.DB.Cases.TryGetValue(cases[UnityEngine.Random.Range(0, cases.Count)].CaseID, out @case) ? @case : null;
        }

        public void CleanMap()
        {
            foreach (ItemRegion region in ItemManager.regions)
            {
                region.items.RemoveAll(k => LevelNavigation.tryGetNavigation(k.point, out byte nav) && nav == Location.NavMesh);
            }

            Stopwatch stopWatch = new();
            stopWatch.Start();
            BarricadeManager.BarricadeRegions.Cast<BarricadeRegion>().SelectMany(k => k.drops).Where(k => LevelNavigation.tryGetNavigation(k.model.transform.position, out byte nav) && nav == Location.NavMesh).Select(k => BarricadeManager.tryGetRegion(k.model.transform, out byte x, out byte y, out ushort plant, out _) ? (k, x, y, plant) : (k, byte.MaxValue, byte.MaxValue, ushort.MaxValue)).ToList().ForEach(k => BarricadeManager.destroyBarricade(k.k, k.Item2, k.Item3, k.Item4));
            stopWatch.Stop();
            Logging.Debug($"Clearing all barricades in a game, that one liner took {stopWatch.ElapsedTicks} ticks, {stopWatch.ElapsedMilliseconds}ms");
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