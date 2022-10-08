using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Data;
using UnturnedBlackout.Models.Feed;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.GameTypes;

public abstract class Game
{
    protected QuestManager Quest => Plugin.Instance.Quest;
    protected UIManager UI => Plugin.Instance.UI;
    protected DatabaseManager DB => Plugin.Instance.DB;
    protected ConfigManager Config => Plugin.Instance.Config;

    public EGameType GameMode { get; set; }
    public ArenaLocation Location { get; set; }
    public bool IsHardcore { get; set; }

    public EGamePhase GamePhase { get; set; }

    public List<Feed> Killfeed { get; set; }

    public List<GamePlayer> PlayersTalking { get; set; }

    public Dictionary<GamePlayer, (BarricadeDrop, KillstreakData)> GameTurrets { get; set; }
    public Dictionary<BarricadeDrop, GamePlayer> GameTurretsInverse { get; set; }
    public Dictionary<BarricadeDrop, Coroutine> GameTurretDamager { get; set; }

    public Coroutine VoteEnder { get; set; }
    public Coroutine KillFeedChecker { get; set; }

    public Game(EGameType gameMode, ArenaLocation location, bool isHardcore)
    {
        GameMode = gameMode;
        Location = location;
        IsHardcore = isHardcore;
        GamePhase = EGamePhase.WAITING_FOR_PLAYERS;
        Killfeed = new();

        GameTurrets = new();
        GameTurretsInverse = new();
        GameTurretDamager = new();

        PlayersTalking = new();

        UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
        PlayerLife.OnSelectingRespawnPoint += OnPlayerRespawning;
        UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
        UnturnedPlayerEvents.OnPlayerInventoryAdded += OnPlayerPickupItem;
        DamageTool.damagePlayerRequested += OnPlayerDamaged;
        ChatManager.onChatted += OnChatted;
        ItemManager.onTakeItemRequested += OnTakeItem;
        UseableThrowable.onThrowableSpawned += OnThrowableSpawned;
        BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
        BarricadeManager.onDamageBarricadeRequested += OnBarricadeDamage;

        UseableConsumeable.onConsumePerformed += OnConsumed;
        UseableGun.OnAimingChanged_Global += OnAimingChanged;

        KillFeedChecker = Plugin.Instance.StartCoroutine(UpdateKillfeed());
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
        BarricadeManager.onDamageBarricadeRequested -= OnBarricadeDamage;
        UseableConsumeable.onConsumePerformed -= OnConsumed;
        UseableGun.OnAimingChanged_Global -= OnAimingChanged;

        if (KillFeedChecker != null)
            Plugin.Instance.StopCoroutine(KillFeedChecker);
    }

    private void OnAimingChanged(UseableGun obj)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(obj.player);
        if (gPlayer == null)
            return;

        // PlayerAimingChanged(gPlayer, obj.isAiming);
    }

    private void OnConsumed(Player instigatingPlayer, ItemConsumeableAsset consumeableAsset)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(instigatingPlayer);
        if (gPlayer == null)
            return;

        PlayerConsumeableUsed(gPlayer, consumeableAsset);
    }

    private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
    {
        CSteamID steamID = new(drop.GetServersideData()?.owner ?? 0UL);
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(steamID);
        if (gPlayer == null)
            return;

        PlayerBarricadeSpawned(gPlayer, drop);
    }

    private void OnBarricadeDamage(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(instigatorSteamID);
        var drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
        if (gPlayer == null || drop == null)
            return;

        PlayerBarricadeDamaged(gPlayer, drop, ref pendingTotalDamage, ref shouldAllow);
    }

    public void OnBarricadeDestroyed(BarricadeDrop drop)
    {
        Logging.Debug($"Barricade destroyed");
        if (!GameTurretsInverse.TryGetValue(drop, out var gPlayer))
            return;

        Logging.Debug($"Drop found to be registered as a turret in the game by {gPlayer.Player.CharacterName}");

        _ = GameTurrets.Remove(gPlayer);
        _ = GameTurretsInverse.Remove(drop);
        if (GameTurretDamager.TryGetValue(drop, out var damager) && damager != null)
        {
            Logging.Debug($"Turret damager coroutine not null, remove");
            Plugin.Instance.StopCoroutine(damager);
        }

        _ = GameTurretDamager.Remove(drop);
    }

    public IEnumerator DamageTurret(BarricadeDrop drop, int healthPerSecond)
    {
        var data = drop.GetServersideData();
        if (data == null)
            yield break;

        while (!data.barricade.isDead)
        {
            yield return new WaitForSeconds(1f);

            BarricadeManager.damage(drop.model.transform, healthPerSecond, 1, false);
        }
    }

    private void OnThrowableSpawned(UseableThrowable useable, GameObject throwable)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(useable.player);
        if (gPlayer == null)
            return;

        PlayerThrowableSpawned(gPlayer, useable);
    }

    public void OnTrapTriggered(GamePlayer player, BarricadeDrop drop)
    {
        var trapOwner = Plugin.Instance.Game.GetGamePlayer(new CSteamID(drop.GetServersideData().owner));
        Logging.Debug($"Trap triggered by {player.Player.CharacterName}, owner is {trapOwner?.Player?.CharacterName ?? "None"}");
        if (trapOwner != null)
        {
            Logging.Debug("Trap owner is not null, adding trap owner to the player's last damager");
            if (player.LastDamager.Count > 0 && player.LastDamager.Peek() == trapOwner.SteamID)
                return;

            player.LastDamager.Push(trapOwner.SteamID);
        }
    }

    private void OnPlayerRespawning(PlayerLife sender, bool wantsToSpawnAtHome, ref Vector3 position, ref float yaw)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(sender.player);
        if (gPlayer == null)
            return;

        OnPlayerRespawn(gPlayer, ref position, ref yaw);
    }

    private void OnTakeItem(Player player, byte x, byte y, uint instanceID, byte to_x, byte to_y, byte to_rot, byte to_page, ItemData itemData, ref bool shouldAllow)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(player);
        if (gPlayer == null)
        {
            shouldAllow = false;
            return;
        }

        OnTakingItem(gPlayer, itemData, ref shouldAllow);
    }

    private void OnChatted(SteamPlayer player, EChatMode mode, ref Color chatted, ref bool isRich, string text, ref bool isVisible)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(player.player);
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
            _ = PlayersTalking.Remove(player);
            OnVoiceChatUpdated(player);
        }
    }

    public void SendVoiceChat(List<GamePlayer> players, bool isTeam)
    {
        var talkingPlayers = PlayersTalking;
        if (isTeam)
            talkingPlayers = PlayersTalking.Where(k => players.Contains(k)).ToList();

        Plugin.Instance.UI.UpdateVoiceChatUI(players, talkingPlayers);
    }

    public void OnStanceChanged(PlayerStance obj) => PlayerStanceChanged(obj);

    public void OnChangeFiremode(GamePlayer player) => PlayerChangeFiremode(player);

    private void OnPlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P) => PlayerPickupItem(player, inventoryGroup, inventoryIndex, P);

    public void OnKill(GamePlayer killer, GamePlayer victim, ushort weaponID, string killerColor, string victimColor)
    {
        if (!Plugin.Instance.UI.KillFeedIcons.TryGetValue(weaponID, out var icon) && weaponID != 0 && weaponID != 1)
            return;

        var feed = weaponID == 0 || weaponID == 1 ? new($"{(victim.Data.HasPrime ? UIManager.PRIME_SYMBOL : "")}<color={victimColor}>{victim.Player.CharacterName.ToUnrich()}</color> {(weaponID == 0 ? "" : "")} ", DateTime.UtcNow) : new Feed(
            $"{(killer.Data.HasPrime ? UIManager.PRIME_SYMBOL : "")}<color={killerColor}>{killer.Player.CharacterName.ToUnrich()}</color> {icon.Symbol} {(victim.Data.HasPrime ? UIManager.PRIME_SYMBOL : "")}<color={victimColor}>{victim.Player.CharacterName.ToUnrich()}</color>", DateTime.UtcNow);

        if (Killfeed.Count < Config.Base.FileData.MaxKillFeed)
        {
            Killfeed.Add(feed);
            OnKillfeedUpdated();
            return;
        }

        var removeKillfeed = Killfeed.OrderBy(k => k.Time).FirstOrDefault();

        _ = Killfeed.Remove(removeKillfeed);
        Killfeed.Add(feed);
        OnKillfeedUpdated();
    }

    public void OnKillfeedUpdated() => Plugin.Instance.UI.SendKillfeed(GetPlayers(), GameMode, Killfeed);

    public IEnumerator UpdateKillfeed()
    {
        while (true)
        {
            yield return new WaitForSeconds(Config.Base.FileData.KillFeedSeconds);

            if (Killfeed.RemoveAll(k => (DateTime.UtcNow - k.Time).TotalSeconds >= Config.Base.FileData.KillFeedSeconds) > 0)
                OnKillfeedUpdated();
        }
    }

    private void OnPlayerDamaged(ref DamagePlayerParameters parameters, ref bool shouldAllow) => OnPlayerDamage(ref parameters, ref shouldAllow);

    private void OnPlayerRevive(UnturnedPlayer player, Vector3 position, byte angle) => OnPlayerRevived(player);

    private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer) => OnPlayerDead(player.Player, murderer, limb, cause);

    public Case GetRandomRoundEndCase()
    {
        var cases = Config.RoundEndCases.FileData.RoundEndCases;
        if (cases.Count == 0)
            return null;

        var poolSize = 0;
        foreach (var roundEndCase in cases)
            poolSize += roundEndCase.Weight;

        var randInt = UnityEngine.Random.Range(0, poolSize) + 1;

        var accumulatedProbability = 0;
        Case @case;
        for (var i = 0; i < cases.Count; i++)
        {
            var roundEndCase = cases[i];
            accumulatedProbability += roundEndCase.Weight;
            if (randInt <= accumulatedProbability)
            {
                if (Plugin.Instance.DB.Cases.TryGetValue(roundEndCase.CaseID, out @case))
                    return @case;

                Logging.Debug($"Case with id {roundEndCase.CaseID} not found for selecting round end case");
                break;
            }
        }

        return Plugin.Instance.DB.Cases.TryGetValue(cases[UnityEngine.Random.Range(0, cases.Count)].CaseID, out @case) ? @case : null;
    }

    public void CleanMap()
    {
        foreach (var region in ItemManager.regions)
            _ = region.items.RemoveAll(k => LevelNavigation.tryGetNavigation(k.point, out var nav) && nav == Location.NavMesh);

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        BarricadeManager.BarricadeRegions.Cast<BarricadeRegion>().SelectMany(k => k.drops).Where(k => LevelNavigation.tryGetNavigation(k.model.transform.position, out var nav) && nav == Location.NavMesh)
            .Select(k => BarricadeManager.tryGetRegion(k.model.transform, out var x, out var y, out var plant, out _) ? (k, x, y, plant) : (k, byte.MaxValue, byte.MaxValue, ushort.MaxValue)).ToList().ForEach(k => BarricadeManager.destroyBarricade(k.k, k.Item2, k.Item3, k.Item4));

        stopWatch.Stop();
        Logging.Debug($"Clearing all barricades in a game, that one liner took {stopWatch.ElapsedTicks} ticks, {stopWatch.ElapsedMilliseconds}ms");
    }

    public abstract void ForceStartGame();
    public abstract void ForceEndGame();
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

    public abstract void PlayerBarricadeDamaged(GamePlayer player, BarricadeDrop drop, ref ushort pendingTotalDamage, ref bool shouldAllow);

    public abstract void PlayerConsumeableUsed(GamePlayer player, ItemConsumeableAsset consumeableAsset);
    public abstract void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible);
    public abstract void OnVoiceChatUpdated(GamePlayer player);
    public abstract void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow);
    public abstract void PlayerEquipmentChanged(GamePlayer player);
    public abstract void PlayerAimingChanged(GamePlayer player, bool isAiming);
    public abstract int GetPlayerCount();
    public abstract bool IsPlayerCarryingFlag(GamePlayer player);
    public abstract List<GamePlayer> GetPlayers();
    public abstract TeamInfo GetTeam(GamePlayer player);
}