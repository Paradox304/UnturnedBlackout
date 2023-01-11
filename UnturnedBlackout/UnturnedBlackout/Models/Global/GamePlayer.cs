using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Animation;

namespace UnturnedBlackout.Models.Global;

public class GamePlayer : IDisposable
{
    private static ConfigManager Config => Plugin.Instance.Config;

    public Game CurrentGame { get; set; }
    public bool StaffMode { get; set; }
    
    public CSteamID SteamID { get; set; }
    public UnturnedPlayer Player { get; set; }
    public PlayerData Data { get; set; }
    public ITransportConnection TransportConnection { get; set; }

    public bool IsLoading { get; set; }

    public bool IsPendingLoadoutChange { get; set; }
    public Loadout ActiveLoadout { get; set; }

    public bool HasScoreboard { get; set; }
    public DateTime ScoreboardCooldown { get; set; }

    public bool HasMidgameLoadout { get; set; }
    public DateTime LastMidgameLoadoutSent { get; set; }
    
    public bool HasSpawnProtection { get; set; }

    public Stack<CSteamID> LastDamager { get; set; }
    public CSteamID LastKiller { get; set; }

    public bool HasAnimationGoingOn { get; set; }
    public List<AnimationInfo> PendingAnimations { get; set; }

    public byte LastEquippedPage { get; set; }
    public byte LastEquippedX { get; set; }
    public byte LastEquippedY { get; set; }
    public bool ForceEquip { get; set; }

    public byte KnifePage { get; set; }
    public byte KnifeX { get; set; }
    public byte KnifeY { get; set; }

    public byte KillstreakPage { get; set; }
    public byte KillstreakX { get; set; }
    public byte KillstreakY { get; set; }

    public ushort KillstreakPreviousShirtID { get; set; }
    public ushort KillstreakPreviousPantsID { get; set; }
    public ushort KillstreakPreviousHatID { get; set; }
    public ushort KillstreakPreviousVestID { get; set; }
    
    public bool HasKillstreakActive { get; set; }
    public LoadoutKillstreak ActiveKillstreak { get; set; }
    public List<LoadoutKillstreak> OrderedKillstreaks { get; set; }
    public Dictionary<LoadoutKillstreak, bool> AvailableKillstreaks { get; set; }
    public Dictionary<ushort, LoadoutKillstreak> KillstreakTriggers { get; set; }
    public int ExtraKillstreak { get; set; }
    
    public bool HasDeathstreakActive { get; set; }
    public int CurrentLifeKills { get; set; }
    public int CurrentDeathstreak { get; set; }
    public int ExtraDeathstreak { get; set; }
    
    public byte AbilityPage { get; set; }
    public byte AbilityX { get; set; }
    public byte AbilityY { get; set; }
    
    public bool HasAbilityActive { get; set; }
    public bool HasAbilityAvailable { get; set; }
    
    public bool HasTactical { get; set; }
    public bool HasLethal { get; set; }

    public float TacticalIntervalSeconds { get; set; }
    public float LethalIntervalSeconds { get; set; }

    public float HealAmount { get; set; }

    public DateTime LastXPPopup { get; set; }
    public int LastXP { get; set; }
    
    public EPlayerStance PreviousStance { get; set; }

    public Coroutine Healer { get; set; }
    public Coroutine RespawnTimer { get; set; }
    public Coroutine VoiceChatChecker { get; set; }
    public Coroutine AnimationChecker { get; set; }
    public Coroutine GadgetGiver { get; set; }
    public Coroutine SpawnProtectionRemover { get; set; }
    public Coroutine DamageChecker { get; set; }
    public Coroutine TacticalChecker { get; set; }
    public Coroutine LethalChecker { get; set; }
    public Coroutine KillstreakChecker { get; set; }
    public Coroutine ItemRemover { get; set; }
    public Coroutine ClothingRemover { get; set; }
    public Coroutine DeathstreakChecker { get; set; }
    public Coroutine AbilityChecker { get; set; }
    public Coroutine EquipmentChecker { get; set; }
    
    public GamePlayer(UnturnedPlayer player, ITransportConnection transportConnection)
    {
        SteamID = player.CSteamID;
        Player = player;
        if (!Plugin.Instance.DB.PlayerData.TryGetValue(SteamID, out var data))
        {
            Provider.kick(SteamID, "Your data was not found, please contact an admin on unturnedblackout.com");
            throw new($"PLAYER DATA FOR PLAYER WITH {SteamID} NOT FOUND, KICKING THE PLAYER");
        }

        StaffMode = false;
        Data = data;
        TransportConnection = transportConnection;
        PreviousStance = EPlayerStance.STAND;
        LastXPPopup = DateTime.UtcNow;
        LastXP = 0;
        LastDamager = new(100);
        PendingAnimations = new();
        ScoreboardCooldown = DateTime.UtcNow;
        LastMidgameLoadoutSent = DateTime.UtcNow;
        AvailableKillstreaks = new();
        KillstreakTriggers = new();
        OrderedKillstreaks = new();
    }

    public void Dispose()
    {
        Logging.Debug($"Game player for {Data.SteamName} is being disposed. Generation: {GC.GetGeneration(this)}", ConsoleColor.Blue);
        CurrentGame = null;
        Player = null;
        Data = null;
        TransportConnection = null;
        ActiveLoadout = null;
        LastDamager = null;
        PendingAnimations = null;
        ActiveKillstreak = null;
        OrderedKillstreaks = null;
        AvailableKillstreaks = null;
        KillstreakTriggers = null;
        
        TacticalChecker.Stop();
        TacticalChecker = null;
        LethalChecker.Stop();
        LethalChecker = null;
        SpawnProtectionRemover.Stop();
        SpawnProtectionRemover = null;
        Healer.Stop();
        Healer = null;
        DamageChecker.Stop();
        DamageChecker = null;
        RespawnTimer.Stop();
        RespawnTimer = null;
        VoiceChatChecker.Stop();
        VoiceChatChecker = null;
        AnimationChecker.Stop();
        AnimationChecker = null;
        GadgetGiver.Stop();
        GadgetGiver = null;
        ItemRemover.Stop();
        ItemRemover = null;
        ClothingRemover.Stop();
        ClothingRemover = null;
        EquipmentChecker.Stop();
        EquipmentChecker = null;
        KillstreakChecker.Stop();
        KillstreakChecker = null;
        DeathstreakChecker.Stop();
        DeathstreakChecker = null;
        AbilityChecker.Stop();
        AbilityChecker = null;
    }
    
    ~GamePlayer()
    {
        Logging.Debug("GamePlayer is being destroyed/finalised", ConsoleColor.Magenta);
    }
    
    // Spawn Protection Seconds
    public void GiveSpawnProtection(int seconds)
    {
        SpawnProtectionRemover.Stop();
        HasSpawnProtection = true;
        SpawnProtectionRemover = Plugin.Instance.StartCoroutine(RemoveSpawnProtection(seconds));
    }

    public IEnumerator RemoveSpawnProtection(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        
        HasSpawnProtection = false;
    }

    // Loadout
    public void SetActiveLoadout(Loadout loadout, byte knifePage, byte knifeX, byte knifeY)
    {
        ActiveLoadout = loadout;

        KnifePage = knifePage;
        KnifeX = knifeX;
        KnifeY = knifeY;

        HasTactical = false;
        HasLethal = false;

        if (loadout == null)
            return;

        LethalChecker.Stop();
        TacticalChecker.Stop();

        var medic = loadout.PerksSearchByType.TryGetValue("medic", out var medicPerk) && (CurrentGame?.GameEvent?.AllowPerks ?? true) ? medicPerk.Perk.SkillLevel : 0f;
        HealAmount = Config.Base.FileData.HealAmount * (1 + medic / 100);

        Plugin.Instance.UI.SendGadgetIcons(this);

        if (loadout.Tactical != null && (CurrentGame?.GameEvent?.AllowTactical ?? true))
        {
            HasTactical = true;
            var tactician = loadout.PerksSearchByType.TryGetValue("tactician", out var tacticianPerk) ? tacticianPerk.Perk.SkillLevel : 0f;
            TacticalIntervalSeconds = loadout.Tactical.Gadget.GiveSeconds * (1 - tactician / 100);
        }

        if (loadout.Lethal != null && (CurrentGame?.GameEvent?.AllowLethal ?? true))
        {
            HasLethal = true;
            var grenadier = loadout.PerksSearchByType.TryGetValue("grenadier", out var grenadierPerk) ? grenadierPerk.Perk.SkillLevel : 0f;
            LethalIntervalSeconds = loadout.Lethal.Gadget.GiveSeconds * (1 - grenadier / 100);
        }

        Plugin.Instance.UI.UpdateGadgetUsed(this, false, !HasLethal);
        Plugin.Instance.UI.UpdateGadgetUsed(this, true, !HasTactical);

        SetupKillstreaks();
        SetupDeathstreaks();
        SetupAbilities();
    }

    // Tactical and Lethal
    public void UsedTactical()
    {
        HasTactical = false;
        Plugin.Instance.UI.UpdateGadgetUsed(this, true, true);
        if (CurrentGame != null)
        {
            Dictionary<EQuestCondition, int> questConditions = new() { { EQuestCondition.MAP, CurrentGame.Location.LocationID }, { EQuestCondition.GAMEMODE, (int)CurrentGame.GameMode }, { EQuestCondition.EVENT_ID, CurrentGame.GameEvent?.EventID ?? 0 }, { EQuestCondition.GADGET, ActiveLoadout.Tactical.Gadget.GadgetID } };
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(this, EQuestType.GADGETS_USED, questConditions));
        }

        TacticalChecker.Stop();
        TacticalChecker = Plugin.Instance.StartCoroutine(EnableTactical());

        GadgetGiver.Stop();
        GadgetGiver = Plugin.Instance.StartCoroutine(GiveGadget(ActiveLoadout.Tactical.Gadget.GadgetID, true));
    }

    public void UsedLethal()
    {
        HasLethal = false;
        Plugin.Instance.UI.UpdateGadgetUsed(this, false, true);
        if (CurrentGame != null)
        {
            Dictionary<EQuestCondition, int> questConditions = new() { { EQuestCondition.MAP, CurrentGame.Location.LocationID }, { EQuestCondition.GAMEMODE, (int)CurrentGame.GameMode }, { EQuestCondition.EVENT_ID, CurrentGame.GameEvent?.EventID ?? 0 }, { EQuestCondition.GADGET, ActiveLoadout.Lethal.Gadget.GadgetID } };
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(this, EQuestType.GADGETS_USED, questConditions));
        }

        LethalChecker.Stop();
        LethalChecker = Plugin.Instance.StartCoroutine(EnableLethal());

        //m_LethalChecker.Start();
        GadgetGiver.Stop();
        GadgetGiver = Plugin.Instance.StartCoroutine(GiveGadget(ActiveLoadout.Lethal.Gadget.GadgetID, false));
    }

    public IEnumerator EnableLethal()
    {
        yield return new WaitForSeconds(LethalIntervalSeconds);

        HasLethal = true;
        Plugin.Instance.UI.UpdateGadgetUsed(this, false, false);
    }

    public IEnumerator EnableTactical()
    {
        yield return new WaitForSeconds(TacticalIntervalSeconds);

        HasTactical = true;
        Plugin.Instance.UI.UpdateGadgetUsed(this, true, false);
    }

    public IEnumerator GiveGadget(ushort id, bool isTactical)
    {
        yield return new WaitForSeconds(1);

        while (true)
        {
            if (Player.Player.life.isDead)
            {
                yield return new WaitForSeconds(1);

                continue;
            }

            var inv = Player.Player.inventory;
            inv.forceAddItem(new(id, false), false);
            inv.TryGetItemIndex(id, out var x, out var y, out var page, out var _);
            if (Assets.find(EAssetType.ITEM, id) is ItemAsset gadgetAsset)
                Player.Player.equipment.ServerBindItemHotkey(Data.GetHotkey(isTactical ? EHotkey.TACTICAL : EHotkey.LETHAL), gadgetAsset, page, x, y);
            break;
        }
    }

    // Healing
    public void OnDamaged(CSteamID damager)
    {
        DamageChecker.Stop();
        Healer.Stop();
        DamageChecker = Plugin.Instance.StartCoroutine(CheckDamage());

        var damagerPlayer = Plugin.Instance.Game.GetGamePlayer(damager);
        if (damagerPlayer == null)
            return;

        if (LastDamager.Count > 0 && LastDamager.Peek() == damager)
            return;

        LastDamager.Push(damager);
    }

    public IEnumerator CheckDamage()
    {
        yield return new WaitForSeconds(Config.Base.FileData.LastDamageAfterHealSeconds);

        Healer = Plugin.Instance.StartCoroutine(HealPlayer());
    }

    public IEnumerator HealPlayer()
    {
        var seconds = Config.Base.FileData.HealSeconds;
        while (true)
        {
            yield return new WaitForSeconds(seconds);

            var health = Player.Player.life.health;
            if (health == 100)
            {
                LastDamager.Clear();
                break;
            }

            Player.Player.life.serverModifyHealth(HealAmount);
        }
    }

    // Kill Card
    public void OnKilled(GamePlayer victim)
    {
        CurrentLifeKills++;
        var victimData = victim.Data;
        Plugin.Instance.UI.SendKillCard(this, victim, victimData);
    }

    // Death screen
    public void OnDeath(CSteamID killer, int respawnSeconds)
    {
        if (HasDeathstreakActive)
            RemoveActiveDeathstreak();
        
        LastXPPopup = LastXPPopup.Subtract(TimeSpan.FromSeconds(30));
        if (HasKillstreakActive)
            RemoveActiveKillstreak();
        
        if (HasAbilityActive)
            RemoveActiveAbility();

        if (!Plugin.Instance.DB.PlayerData.TryGetValue(killer, out var killerData))
        {
            TaskDispatcher.QueueOnMainThread(() => Player.Player.life.ServerRespawn(false));
            return;
        }

        if (killer != SteamID)
            LastKiller = killer;

        DamageChecker.Stop();
        Healer.Stop();

        Plugin.Instance.UI.RemoveKillCard(this);

        var killerPlayer = Plugin.Instance.Game.GetGamePlayer(killer);
        if (killerPlayer == null)
            return;

        Plugin.Instance.UI.SendDeathUI(this, killerPlayer, killerData);
        PreviousStance = EPlayerStance.STAND;
        RespawnTimer.Stop();
        RespawnTimer = Plugin.Instance.StartCoroutine(RespawnTime(respawnSeconds));
    }

    public IEnumerator RespawnTime(int respawnSeconds)
    {
        for (var seconds = respawnSeconds; seconds >= 0; seconds--)
        {
            yield return new WaitForSeconds(1);

            Plugin.Instance.UI.UpdateRespawnTimer(this, $"{seconds}s");
        }

        Player.Player.life.ServerRespawn(false);
    }

    // Equipping and refilling on guns on respawn
    public void OnRevived()
    {
        RespawnTimer.Stop();

        // Remove any left over turrets in the player's inventory
        for (var i = 0; i <= Player.Player.inventory.getItemCount(PlayerInventory.SLOTS); i++)
        {
            var item = Player.Player.inventory.getItem(PlayerInventory.SLOTS, (byte)i);
            if (item != null && ActiveLoadout.Killstreaks.Exists(k => k.Killstreak.KillstreakInfo.IsTurret && item.item.id == k.Killstreak.KillstreakInfo.TurretID))
            {
                Player.Player.inventory.removeItem(PlayerInventory.SLOTS, (byte)i);
                break;
            }
        }

        // Check if loadout needs to be changed, if so change it
        if (IsPendingLoadoutChange)
        {
            Plugin.Instance.Loadout.GiveLoadout(this);
            IsPendingLoadoutChange = false;
            Plugin.Instance.UI.ClearDeathUI(this);

            // Check if deathstreak needs to be started
            if (CurrentLifeKills == 0)
                UpdateDeathstreak(++CurrentDeathstreak);
            else
                CurrentDeathstreak = 0;

            CurrentLifeKills = 0;
            return;
        }

        // Check if deathstreak needs to be started
        if (CurrentLifeKills == 0)
            UpdateDeathstreak(++CurrentDeathstreak);
        else
            CurrentDeathstreak = 0;

        CurrentLifeKills = 0;
        
        // Fill up the guns
        for (byte i = 0; i <= 1; i++)
        {
            var item = Player.Player.inventory.getItem(i, 0);
            if (item != null && item.item.state.Length > 8)
            {
                var magID = BitConverter.ToUInt16(item.item.state, 8);
                if (Assets.find(EAssetType.ITEM, magID) is ItemMagazineAsset mAsset)
                    item.item.state[10] = mAsset.amount;
            }
        }

        Player.Player.equipment.ServerEquip(LastEquippedPage, LastEquippedX, LastEquippedY);
        Plugin.Instance.UI.ClearDeathUI(this);
    }

    // Stance changing
    public void OnStanceChanged(EPlayerStance newStance)
    {
        TaskDispatcher.QueueOnMainThread(() =>
        {
            if (PreviousStance == EPlayerStance.CLIMB && newStance != EPlayerStance.CLIMB)
            {
                if (HasKillstreakActive && ActiveKillstreak.Killstreak.KillstreakInfo.IsItem)
                    Player.Player.equipment.ServerEquip(KillstreakPage, KillstreakX, KillstreakY);
                else
                    Player.Player.equipment.ServerEquip(LastEquippedPage, LastEquippedX, LastEquippedY);
            }

            PreviousStance = newStance;
        });
    }

    // Voice chat
    public void OnTalking()
    {
        if (VoiceChatChecker != null)
            Plugin.Instance.StopCoroutine(VoiceChatChecker);
        else
            CurrentGame?.OnStartedTalking(this);

        VoiceChatChecker = Plugin.Instance.StartCoroutine(CheckVoiceChat());
    }

    public IEnumerator CheckVoiceChat()
    {
        yield return new WaitForSeconds(0.5f);

        if (CurrentGame != null && !Player.Player.voice.isTalking)
            CurrentGame.OnStoppedTalking(this);

        VoiceChatChecker = null;
    }

    // Animation
    public IEnumerator CheckAnimation()
    {
        HasAnimationGoingOn = true;
        yield return new WaitForSeconds(5);

        HasAnimationGoingOn = false;

        if (PendingAnimations.Count > 0)
        {
            Plugin.Instance.UI.SendAnimation(this, PendingAnimations[0]);
            PendingAnimations.RemoveAt(0);
        }
    }

    // Killstreak
    public void SetupKillstreaks()
    {
        HasKillstreakActive = false;
        ActiveKillstreak = null;

        AvailableKillstreaks = new();
        KillstreakTriggers = new();
        OrderedKillstreaks = new();

        ExtraKillstreak = ActiveLoadout.PerksSearchByType.TryGetValue("expert", out var expertPerk) && (CurrentGame?.GameEvent?.AllowPerks ?? true) ? expertPerk.Perk.SkillLevel : 0;

        if (CurrentGame?.GameEvent?.AllowKillstreaks ?? true)
        {
            foreach (var killstreak in ActiveLoadout.Killstreaks.OrderBy(k => k.Killstreak.KillstreakRequired))
            {
                OrderedKillstreaks.Add(killstreak);
                AvailableKillstreaks.Add(killstreak, false);
                KillstreakTriggers.Add(killstreak.Killstreak.KillstreakInfo.TriggerItemID, killstreak);
            }
        }

        if (OrderedKillstreaks.Count == 0)
            Plugin.Instance.UI.ClearKillstreakUI(this);
        else
            Plugin.Instance.UI.SetupKillstreakUI(this);

        if (ExtraKillstreak > 0)
            Plugin.Instance.UI.UpdateKillstreakBars(this, ExtraKillstreak);
    }

    public void UpdateKillstreak(int currentKillstreak)
    {
        var updatedKillstreak = currentKillstreak + ExtraKillstreak;
        var availableKillstreak = OrderedKillstreaks.FirstOrDefault(k => k.Killstreak.KillstreakRequired == updatedKillstreak && !AvailableKillstreaks[k]);
        if (availableKillstreak != null)
        {
            AvailableKillstreaks[availableKillstreak] = true;
            Plugin.Instance.UI.UpdateKillstreakReady(this, availableKillstreak);
            Plugin.Instance.UI.SendAnimation(this, new(EAnimationType.KILLSTREAK_AVAILABLE, availableKillstreak.Killstreak));
        }

        Plugin.Instance.UI.UpdateKillstreakBars(this, updatedKillstreak);
    }

    public void ActivateKillstreak(LoadoutKillstreak killstreak)
    {
        var info = killstreak.Killstreak.KillstreakInfo;
        var inv = Player.Player.inventory;
        if (CurrentGame == null)
            return;

        if (info.IsItem)
        {
            if (info.MagAmount > 0)
            {
                for (var i = 1; i <= info.MagAmount; i++)
                    inv.forceAddItem(new(info.MagID, true), false);
            }

            inv.forceAddItem(new(info.ItemID, true), false);
            if (!inv.TryGetItemIndex(info.ItemID, out var x, out var y, out var page, out var _))
            {
                Logging.Debug($"Failed to add killstreak to inventory, no space probably?");
                return;
            }

            KillstreakPage = page;
            KillstreakX = x;
            KillstreakY = y;

            Player.Player.equipment.ServerEquip(KillstreakPage, KillstreakX, KillstreakY);
        }
        else if (info.IsTurret)
        {
            if (CurrentGame.GameTurrets.ContainsKey(this))
            {
                Logging.Debug($"Player already has a placed turret in the game, returning");
                return;
            }

            inv.forceAddItem(new(info.TurretID, true), false);

            if (!inv.TryGetItemIndex(info.TurretID, out var x, out var y, out var page, out var _))
            {
                Logging.Debug($"Failed to add turret to inventory, probably no space?");
                return;
            }

            Player.Player.equipment.ServerEquip(page, x, y);

            AvailableKillstreaks[killstreak] = false;
            Plugin.Instance.UI.UpdateKillstreakReady(this, killstreak);

            return;
        }

        if (info.IsClothing)
        {
            var clothing = Player.Player.clothing;
            var clothes = clothing.thirdClothes;
            var clothingKillstreak = CurrentGame.GetTeam(this).TeamKillstreaks.FirstOrDefault(k => k.KillstreakID == killstreak.Killstreak.KillstreakID);

            KillstreakPreviousShirtID = clothing.shirt;
            KillstreakPreviousPantsID = clothing.pants;
            KillstreakPreviousHatID = clothing.hat;
            KillstreakPreviousVestID = clothing.vest;

            if (clothingKillstreak.ShirtID != 0)
            {
                clothes.shirt = 0;
                clothing.askWearShirt(0, 0, new byte[0], false);
                inv.forceAddItem(new(clothingKillstreak.ShirtID, true), true);
            }

            if (clothingKillstreak.PantsID != 0)
            {
                clothes.pants = 0;
                clothing.askWearPants(0, 0, new byte[0], false);
                inv.forceAddItem(new(clothingKillstreak.PantsID, true), true);
            }

            if (clothingKillstreak.HatID != 0)
            {
                clothes.hat = 0;
                clothing.askWearHat(0, 0, new byte[0], false);
                inv.forceAddItem(new(clothingKillstreak.HatID, true), true);
            }

            if (clothingKillstreak.VestID != 0)
            {
                clothes.vest = 0;
                clothing.askWearVest(0, 0, new byte[0], false);
                inv.forceAddItem(new(clothingKillstreak.VestID, true), true);
            }
        }

        KillstreakChecker.Stop();

        HasKillstreakActive = true;
        ActiveKillstreak = killstreak;
        AvailableKillstreaks[killstreak] = false;

        Plugin.Instance.UI.UpdateKillstreakReady(this, killstreak);

        if (info.KillstreakStaySeconds == 0)
            return;

        Plugin.Instance.UI.SendKillstreakTimer(this, info.KillstreakStaySeconds);
        KillstreakChecker = Plugin.Instance.StartCoroutine(CheckKillstreak(info.KillstreakStaySeconds));
    }

    public void RemoveActiveKillstreak()
    {
        Logging.Debug($"Removing killstreak for {Player.CharacterName} with id {ActiveKillstreak.Killstreak.KillstreakID}");
        if (!HasKillstreakActive)
        {
            Logging.Debug($"{Player.CharacterName} has no active killstreak, what we tryna remove");
            return;
        }

        var info = ActiveKillstreak.Killstreak.KillstreakInfo;
        KillstreakChecker.Stop();
        ItemRemover.Stop();
        ClothingRemover.Stop();

        if (info.IsItem)
            ItemRemover = Plugin.Instance.StartCoroutine(RemoveItems(KillstreakPage, KillstreakX, KillstreakY, info.MagID));

        if (info.IsClothing)
            ClothingRemover = Plugin.Instance.StartCoroutine(RemoveClothing(KillstreakPreviousShirtID, KillstreakPreviousPantsID, KillstreakPreviousHatID, KillstreakPreviousVestID));

        Plugin.Instance.UI.ClearKillstreakTimer(this);
        HasKillstreakActive = false;
        ActiveKillstreak = null;
    }

    public IEnumerator RemoveItems(byte page, byte x, byte y, ushort magID)
    {
        while (true)
        {
            if (Player.Player.life.isDead)
            {
                yield return new WaitForSeconds(1f);

                continue;
            }

            var inv = Player.Player.inventory;
            inv.removeItem(page, Player.Player.inventory.getIndex(page, x, y));

            if (magID != 0)
            {
                var itemCount = inv.getItemCount(PlayerInventory.SLOTS);
                for (var i = itemCount - 1; i >= 0; i--)
                {
                    var item = inv.getItem(PlayerInventory.SLOTS, (byte)i);
                    if ((item?.item?.id ?? 0) == magID)
                        inv.removeItem(PlayerInventory.SLOTS, (byte)i);
                }
            }

            break;
        }
    }

    public IEnumerator RemoveClothing(ushort shirt, ushort pants, ushort hat, ushort vest)
    {
        while (true)
        {
            if (Player.Player.life.isDead)
            {
                yield return new WaitForSeconds(1f);

                continue;
            }

            var clothing = Player.Player.clothing;
            var clothes = clothing.thirdClothes;

            if (shirt != 0 && clothing.shirt != shirt)
            {
                clothes.shirt = 0;
                clothing.askWearShirt(0, 0, new byte[0], false);
                Player.Player.inventory.forceAddItem(new(shirt, true), true);
            }

            if (pants != 0 && clothing.pants != pants)
            {
                clothes.pants = 0;
                clothing.askWearPants(0, 0, new byte[0], false);
                Player.Player.inventory.forceAddItem(new(pants, true), true);
            }

            if (hat != 0 && clothing.hat != hat)
            {
                clothes.hat = 0;
                clothing.askWearHat(0, 0, new byte[0], false);
                Player.Player.inventory.forceAddItem(new(hat, true), true);
            }

            if (vest != 0 && clothing.vest != vest)
            {
                clothes.vest = 0;
                clothing.askWearVest(0, 0, new byte[0], false);
                Player.Player.inventory.forceAddItem(new(vest, true), true);
            }

            break;
        }
    }

    public IEnumerator CheckKillstreak(int seconds)
    {
        for (var i = seconds; i > 0; i--)
        {
            Plugin.Instance.UI.UpdateKillstreakTimer(this, i);
            yield return new WaitForSeconds(1f);
        }

        RemoveActiveKillstreak();
    }

    // Deathstreak
    public void SetupDeathstreaks()
    {
        HasDeathstreakActive = false;
        ExtraDeathstreak = ActiveLoadout.PerksSearchByType.TryGetValue("noob", out var noobPerk) && (CurrentGame?.GameEvent?.AllowPerks ?? true) ? noobPerk.Perk.SkillLevel : 0;
    }

    public void UpdateDeathstreak(int currentDeathstreak)
    {
        if (!(CurrentGame?.GameEvent?.AllowDeathstreaks ?? true))
            return;

        CurrentDeathstreak = currentDeathstreak + ExtraDeathstreak;
        if (ActiveLoadout?.Deathstreak != null && CurrentDeathstreak >= ActiveLoadout.Deathstreak.Deathstreak.DeathstreakRequired)
            ActivateDeathstreak();
    }

    public void ActivateDeathstreak()
    {
        HasDeathstreakActive = true;
        var info = ActiveLoadout.Deathstreak.Deathstreak.DeathstreakInfo;
        var movement = Player.Player.movement;
        if (info.SpeedMultiplier != 0f)
            movement.sendPluginSpeedMultiplier(movement.pluginSpeedMultiplier + info.SpeedMultiplier);

        Plugin.Instance.UI.SetupActiveDeathstreakUI(this);
        DeathstreakChecker = Plugin.Instance.StartCoroutine(CheckDeathstreak(info.DeathstreakStaySeconds));
    }
    
    public void RemoveActiveDeathstreak()
    {
        DeathstreakChecker.Stop();
        HasDeathstreakActive = false;
        var info = ActiveLoadout.Deathstreak.Deathstreak.DeathstreakInfo;
        var movement = Player.Player.movement;
        if (info.SpeedMultiplier != 0f)
            movement.sendPluginSpeedMultiplier(movement.pluginSpeedMultiplier - info.SpeedMultiplier);
        
        Plugin.Instance.UI.RemoveActiveDeathstreakUI(this);
    }

    public IEnumerator CheckDeathstreak(int seconds)
    {
        for (var i = seconds; i > 0; i--)
        {
            Plugin.Instance.UI.UpdateDeathstreakTimer(this, i);
            yield return new WaitForSeconds(1f);
        }

        RemoveActiveDeathstreak();
    }
    
    // Ability
    
    public void SetupAbilities()
    {
        HasAbilityActive = false;
        HasAbilityAvailable = false;

        if (ActiveLoadout.Ability == null)
        {
            Plugin.Instance.UI.RemoveAbilityUI(this);
            return;
        }
        
        Plugin.Instance.UI.SendAbilityUI(this);
        if (CurrentGame.GamePhase != EGamePhase.STARTED)
            return;

        AbilityChecker.Stop();
        AbilityChecker = Plugin.Instance.StartCoroutine(CheckAbility(ActiveLoadout.Ability.Ability.AbilityInfo.CooldownSeconds, false));
    }

    public void StartAbilityTimer()
    {
        if (ActiveLoadout.Ability == null || CurrentGame.GamePhase != EGamePhase.STARTED)
            return;
        
        AbilityChecker.Stop();
        AbilityChecker = Plugin.Instance.StartCoroutine(CheckAbility(ActiveLoadout.Ability.Ability.AbilityInfo.CooldownSeconds, false));
    }
    
    public void SetAbilityAvailable()
    {
        HasAbilityAvailable = true;
        Plugin.Instance.UI.UpdateAbilityReady(this);
    }

    public void ActivateAbility()
    {
        var info = ActiveLoadout.Ability.Ability.AbilityInfo;
        var inv = Player.Player.inventory;
        
        if (info.MagAmount > 0)
        {
            for (var i = 1; i <= info.MagAmount; i++)
                inv.forceAddItem(new(info.MagID, true), false);
        }

        inv.forceAddItem(new(info.ItemID, true), false);
        if (!inv.TryGetItemIndex(info.ItemID, out var x, out var y, out var page, out var _))
        {
            Logging.Debug($"Failed to add ability to inventory, no space probably?");
            return;
        }

        AbilityPage = page;
        AbilityX = x;
        AbilityY = y;

        Player.Player.equipment.ServerEquip(AbilityPage, AbilityX, AbilityY);

        AbilityChecker.Stop();
        HasAbilityActive = true;
        HasAbilityAvailable = false;
        AbilityChecker = Plugin.Instance.StartCoroutine(CheckAbility(info.AbilityStaySeconds, true));
    }

    public void RemoveActiveAbility()
    {
        Logging.Debug($"Removing ability for {Player.CharacterName} with id {ActiveLoadout.Ability.Ability.AbilityID}");
        if (!HasAbilityActive)
        {
            Logging.Debug($"{Player.CharacterName} has no active ability, what we tryna remove");
            return;
        }

        var info = ActiveLoadout.Ability.Ability.AbilityInfo;
        AbilityChecker.Stop();
        ItemRemover.Stop();

        if (info.IsItem)
            ItemRemover = Plugin.Instance.StartCoroutine(RemoveItems(AbilityPage, AbilityX, AbilityY, info.MagID));

        Plugin.Instance.UI.UpdateAbilityReady(this);
        HasAbilityActive = false;
        AbilityChecker = Plugin.Instance.StartCoroutine(CheckAbility(ActiveLoadout.Ability.Ability.AbilityInfo.CooldownSeconds, false));
    }
    
    public IEnumerator CheckAbility(int seconds, bool isBeingUsed)
    {
        for (var i = seconds; i > 0; i--)
        {
            Plugin.Instance.UI.UpdateAbilityTimer(this, i, isBeingUsed);
            yield return new WaitForSeconds(1);
        }

        if (isBeingUsed)
            RemoveActiveAbility();
        else
            SetAbilityAvailable();
    }
    
    // Checkers
    public IEnumerator CheckEquipment()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.5f);
            
            if (Player.Player.life.isDead)
                continue;

            if (CurrentGame == null)
                break;

            if (!CurrentGame.IsPlayerCarryingFlag(this))
                continue;

            var equip = Player.Player.equipment;
            if (equip.itemID != (ActiveLoadout.PrimarySkin?.SkinID ?? ActiveLoadout.Primary?.Gun.GunID ?? 0))
                continue;

            if (equip.isBusy || !equip.canEquip)
                continue;

            if (Player.Player.inventory.getItem(1, 0) != null)
            {
                equip.ServerEquip(1, 0, 0);
                continue;
            }
            
            equip.ServerEquip(KnifePage, KnifeX, KnifeY);
        }
    }
    
    // Events
    public void OnGameJoined(Game game)
    {
        CurrentGame = game;
        IsLoading = true;
        
        if (game.GameMode == EGameType.CTF)
            EquipmentChecker = Plugin.Instance.StartCoroutine(CheckEquipment());
    }

    public void OnGameLeft()
    {
        CurrentGame = null;
        IsLoading = false;
        
        CurrentLifeKills = 0;
        CurrentDeathstreak = 0;
        
        Healer.Stop();
        RespawnTimer.Stop();
        VoiceChatChecker.Stop();
        AnimationChecker.Stop();
        GadgetGiver.Stop();
        SpawnProtectionRemover.Stop();
        DamageChecker.Stop();
        TacticalChecker.Stop();
        LethalChecker.Stop();
        KillstreakChecker.Stop();
        ItemRemover.Stop();
        ClothingRemover.Stop();
        DeathstreakChecker.Stop();
        AbilityChecker.Stop();
        EquipmentChecker.Stop();

        HasScoreboard = false;
        HasAnimationGoingOn = false;
        IsPendingLoadoutChange = false;
        LastKiller = CSteamID.Nil;
        LastDamager.Clear();
        PendingAnimations.Clear();
        Plugin.Instance.UI.ClearDeathUI(this);
    }
}