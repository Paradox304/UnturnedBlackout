using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Animation;

namespace UnturnedBlackout.Models.Global
{
    public class GamePlayer
    {
        public ConfigManager Config
        {
            get
            {
                return Plugin.Instance.Config;
            }
        }

        public Game CurrentGame { get; set; }

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

        public bool HasTactical { get; set; }
        public bool HasLethal { get; set; }

        public float TacticalIntervalSeconds { get; set; }
        public float LethalIntervalSeconds { get; set; }

        public bool HasKillstreakActive { get; set; }
        public LoadoutKillstreak ActiveKillstreak { get; set; }
        public List<LoadoutKillstreak> OrderedKillstreaks { get; set; }
        public Dictionary<LoadoutKillstreak, bool> AvailableKillstreaks { get; set; }
        public Dictionary<ushort, LoadoutKillstreak> KillstreakTriggers { get; set; }
        public int ExtraKillstreak { get; set; }

        public float PrimaryMovementChange { get; set; }
        public float PrimaryMovementChangeADS { get; set; }
        public float SecondaryMovementChange { get; set; }
        public float SecondaryMovementChangeADS { get; set; }
        public float KnifeMovementChange { get; set; }

        public float HealAmount { get; set; }

        public EPlayerStance PreviousStance { get; set; }

        public Coroutine MovementChanger { get; set; }
        public Coroutine Healer { get; set; }
        public Coroutine RespawnTimer { get; set; }
        public Coroutine VoiceChatChecker { get; set; }
        public Coroutine AnimationChecker { get; set; }
        public Coroutine KillstreakItemRemover { get; set; }
        public Coroutine GadgetGiver { get; set; }
        public Coroutine SpawnProtectionRemover { get; set; }
        public Coroutine DamageChecker { get; set; }
        public Coroutine TacticalChecker { get; set; }
        public Coroutine LethalChecker { get; set; }
        public Coroutine KillstreakChecker { get; set; }

        public GamePlayer(UnturnedPlayer player, ITransportConnection transportConnection)
        {
            SteamID = player.CSteamID;
            Player = player;
            if (!Plugin.Instance.DB.PlayerData.TryGetValue(SteamID, out PlayerData data))
            {
                Provider.kick(SteamID, "Your data was not found, please contact an admin on unturnedblackout.com");
                throw new Exception($"PLAYER DATA FOR PLAYER WITH {SteamID} NOT FOUND, KICKING THE PLAYER");
            }
            Data = data;
            TransportConnection = transportConnection;
            PreviousStance = EPlayerStance.STAND;
            LastDamager = new Stack<CSteamID>(100);
            PendingAnimations = new();
            ScoreboardCooldown = DateTime.UtcNow;

            AvailableKillstreaks = new();
            KillstreakTriggers = new();
            OrderedKillstreaks = new();

        }

        // Spawn Protection Seconds
        public void GiveSpawnProtection(int seconds)
        {

            Logging.Debug($"Giving {Player.CharacterName} spawn protection for {seconds} seconds at {DateTime.UtcNow}");
            SpawnProtectionRemover.Stop();
            HasSpawnProtection = true;
            SpawnProtectionRemover = Plugin.Instance.StartCoroutine(RemoveSpawnProtection(seconds));
        }

        public IEnumerator RemoveSpawnProtection(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            Logging.Debug($"Timer to remove spawn protection for {Player.CharacterName} has passed at {DateTime.UtcNow} removing spawn protection");
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
            {
                return;
            }

            loadout.GetPrimaryMovement(out float primaryMovementChange, out float primaryMovementChangeADS);
            PrimaryMovementChange = primaryMovementChange;
            PrimaryMovementChangeADS = primaryMovementChangeADS;

            loadout.GetSecondaryMovement(out float secondaryMovementChange, out float secondaryMovementChangeADS);
            SecondaryMovementChange = secondaryMovementChange;
            SecondaryMovementChangeADS = secondaryMovementChangeADS;

            KnifeMovementChange = primaryMovementChange + secondaryMovementChange + loadout.GetKnifeMovement();

            Logging.Debug($"Setting movement for {Player.CharacterName} with PrimaryMovementChange {PrimaryMovementChange}, PrimaryMovementChangeADS {PrimaryMovementChangeADS}, SecondaryMovementChange {SecondaryMovementChange}, SecondaryMovementChangeADS {SecondaryMovementChangeADS}, KnifeMovementChange {KnifeMovementChange}");
            LethalChecker.Stop();
            TacticalChecker.Stop();

            float medic = loadout.PerksSearchByType.TryGetValue("medic", out LoadoutPerk medicPerk) ? medicPerk.Perk.SkillLevel : 0f;
            HealAmount = Config.Base.FileData.HealAmount * (1 + (medic / 100));

            Plugin.Instance.UI.SendGadgetIcons(this);

            if (loadout.Tactical != null)
            {
                HasTactical = true;
                float tactician = loadout.PerksSearchByType.TryGetValue("tactician", out LoadoutPerk tacticianPerk) ? tacticianPerk.Perk.SkillLevel : 0f;
                TacticalIntervalSeconds = loadout.Tactical.Gadget.GiveSeconds * (1 - (tactician / 100));
            }

            if (loadout.Lethal != null)
            {
                HasLethal = true;
                float grenadier = loadout.PerksSearchByType.TryGetValue("grenadier", out LoadoutPerk grenadierPerk) ? grenadierPerk.Perk.SkillLevel : 0f;
                LethalIntervalSeconds = loadout.Lethal.Gadget.GiveSeconds * (1 - (grenadier / 100));
            }

            Plugin.Instance.UI.UpdateGadgetUsed(this, false, !HasLethal);
            Plugin.Instance.UI.UpdateGadgetUsed(this, true, !HasTactical);

            SetupKillstreaks();
        }

        // Tactical and Lethal

        public void UsedTactical()
        {
            HasTactical = false;
            Plugin.Instance.UI.UpdateGadgetUsed(this, true, true);
            if (CurrentGame != null)
            {
                Dictionary<EQuestCondition, int> questConditions = new()
                {
                    { EQuestCondition.Map, CurrentGame.Location.LocationID },
                    { EQuestCondition.Gamemode, (int)CurrentGame.GameMode },
                    { EQuestCondition.Gadget, ActiveLoadout.Tactical.Gadget.GadgetID }
                };
                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(this, EQuestType.GadgetsUsed, questConditions));
            }

            TacticalChecker.Stop();
            TacticalChecker = Plugin.Instance.StartCoroutine(EnableTactical());

            GadgetGiver.Stop();
            GadgetGiver = Plugin.Instance.StartCoroutine(GiveGadget(ActiveLoadout.Tactical.Gadget.GadgetID));
        }

        public void UsedLethal()
        {
            HasLethal = false;
            Plugin.Instance.UI.UpdateGadgetUsed(this, false, true);
            if (CurrentGame != null)
            {
                Dictionary<EQuestCondition, int> questConditions = new()
                {
                    { EQuestCondition.Map, CurrentGame.Location.LocationID },
                    { EQuestCondition.Gamemode, (int)CurrentGame.GameMode },
                    { EQuestCondition.Gadget, ActiveLoadout.Lethal.Gadget.GadgetID }
                };
                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(this, EQuestType.GadgetsUsed, questConditions));
            }

            LethalChecker.Stop();
            LethalChecker = Plugin.Instance.StartCoroutine(EnableLethal());

            //m_LethalChecker.Start();
            GadgetGiver.Stop();
            GadgetGiver = Plugin.Instance.StartCoroutine(GiveGadget(ActiveLoadout.Lethal.Gadget.GadgetID));
        }

        public IEnumerator EnableLethal()
        {
            Logging.Debug($"Enabling lethal for {Player.CharacterName}, time to wait {LethalIntervalSeconds}");
            yield return new WaitForSeconds(LethalIntervalSeconds);
            Logging.Debug($"Waited enough, giving lethal");
            HasLethal = true;
            Plugin.Instance.UI.UpdateGadgetUsed(this, false, false);
        }

        public IEnumerator EnableTactical()
        {
            Logging.Debug($"Enabling tactical for {Player.CharacterName}, time to wait {LethalIntervalSeconds}");
            yield return new WaitForSeconds(LethalIntervalSeconds);
            Logging.Debug($"Waited enough, giving tactical");
            HasTactical = true;
            Plugin.Instance.UI.UpdateGadgetUsed(this, true, false);
        }

        public IEnumerator GiveGadget(ushort id)
        {
            yield return new WaitForSeconds(1);
            while (true)
            {
                if (Player.Player.life.isDead)
                {
                    yield return new WaitForSeconds(1);
                    continue;
                }
                Player.Player.inventory.forceAddItem(new Item(id, false), false);
                break;
            }
        }

        // Healing

        public void OnDamaged(CSteamID damager)
        {
            DamageChecker.Stop();
            Healer.Stop();
            DamageChecker = Plugin.Instance.StartCoroutine(CheckDamage());

            GamePlayer damagerPlayer = Plugin.Instance.Game.GetGamePlayer(damager);
            if (damagerPlayer == null)
            {
                return;
            }

            if (LastDamager.Count > 0 && LastDamager.Peek() == damager)
            {
                return;
            }

            LastDamager.Push(damager);
        }

        public IEnumerator CheckDamage()
        {
            yield return new WaitForSeconds(Config.Base.FileData.LastDamageAfterHealSeconds);
            Healer = Plugin.Instance.StartCoroutine(HealPlayer());
        }

        public IEnumerator HealPlayer()
        {
            float seconds = Config.Base.FileData.HealSeconds;
            while (true)
            {
                yield return new WaitForSeconds(seconds);
                byte health = Player.Player.life.health;
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
            PlayerData victimData = victim.Data;
            Plugin.Instance.UI.SendKillCard(this, victim, victimData);
        }

        // Death screen

        public void OnDeath(CSteamID killer, int respawnSeconds)
        {
            if (HasKillstreakActive)
            {
                RemoveActiveKillstreak();
            }

            if (!Plugin.Instance.DB.PlayerData.TryGetValue(killer, out PlayerData killerData))
            {
                TaskDispatcher.QueueOnMainThread(() => Player.Player.life.ServerRespawn(false));
                return;
            }

            if (killer != SteamID)
            {
                LastKiller = killer;
            }

            DamageChecker.Stop();
            Healer.Stop();

            Plugin.Instance.UI.RemoveKillCard(this);

            GamePlayer killerPlayer = Plugin.Instance.Game.GetGamePlayer(killer);
            if (killer == null)
            {
                return;
            }

            Plugin.Instance.UI.SendDeathUI(this, killerPlayer, killerData);
            PreviousStance = EPlayerStance.STAND;
            RespawnTimer.Stop();
            RespawnTimer = Plugin.Instance.StartCoroutine(RespawnTime(respawnSeconds));
        }

        public IEnumerator RespawnTime(int respawnSeconds)
        {
            for (int seconds = respawnSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                Plugin.Instance.UI.UpdateRespawnTimer(this, $"{seconds}s");
            }
            Player.Player.life.ServerRespawn(false);
        }

        // Equipping and refilling on guns on respawn

        public void OnRevived(Kit kit, List<TeamGlove> gloves)
        {
            RespawnTimer.Stop();

            if (IsPendingLoadoutChange)
            {
                Plugin.Instance.Loadout.GiveLoadout(this, kit, gloves);
                IsPendingLoadoutChange = false;
                Plugin.Instance.UI.ClearDeathUI(this);
                return;
            }

            for (byte i = 0; i <= 1; i++)
            {
                ItemJar item = Player.Player.inventory.getItem(i, 0);
                if (item != null && item.item.state.Length > 8)
                {
                    ushort magID = BitConverter.ToUInt16(item.item.state, 8);
                    if (Assets.find(EAssetType.ITEM, magID) is ItemMagazineAsset mAsset)
                    {
                        item.item.state[10] = mAsset.amount;
                    }
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
                    if (!HasKillstreakActive)
                        Player.Player.equipment.ServerEquip(LastEquippedPage, LastEquippedX, LastEquippedY);
                    else
                        Player.Player.equipment.ServerEquip(KillstreakPage, KillstreakX, KillstreakY);
                }
                PreviousStance = newStance;
            });
        }

        // Voice chat

        public void OnTalking()
        {
            if (VoiceChatChecker != null)
            {
                Plugin.Instance.StopCoroutine(VoiceChatChecker);
            }
            else if (CurrentGame != null)
            {
                CurrentGame.OnStartedTalking(this);
            }

            VoiceChatChecker = Plugin.Instance.StartCoroutine(CheckVoiceChat());
        }

        public IEnumerator CheckVoiceChat()
        {
            yield return new WaitForSeconds(0.5f);
            if (CurrentGame != null && !Player.Player.voice.isTalking)
            {
                CurrentGame.OnStoppedTalking(this);
            }

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

        // Movement

        public void GiveMovement(bool isADS, bool isCarryingFlag, bool doSteps)
        {
            if (ActiveLoadout == null)
            {
                return;
            }

            if (Player.Player.equipment.itemID == 0)
            {
                return;
            }

            float flagCarryingSpeed = isCarryingFlag ? Config.CTF.FileData.FlagCarryingSpeed : 0f;
            float updatedMovement;
            if (isCarryingFlag)
            {
                updatedMovement = Config.CTF.FileData.FlagCarryingSpeed;
            }
            else if (Player.Player.equipment.itemID == (ActiveLoadout.Primary?.Gun?.GunID ?? 0) || Player.Player.equipment.itemID == (ActiveLoadout.PrimarySkin?.SkinID ?? 0))
            {
                updatedMovement = PrimaryMovementChange + SecondaryMovementChange + (isADS ? PrimaryMovementChangeADS : 0) + flagCarryingSpeed;
            }
            else if (Player.Player.equipment.itemID == (ActiveLoadout.Secondary?.Gun?.GunID ?? 0) || Player.Player.equipment.itemID == (ActiveLoadout.SecondarySkin?.SkinID ?? 0))
            {
                updatedMovement = PrimaryMovementChange + SecondaryMovementChange + (isADS ? SecondaryMovementChangeADS : 0) + flagCarryingSpeed;
            }
            else if (Player.Player.equipment.itemID == (ActiveLoadout.Knife.Knife.KnifeID))
            {
                updatedMovement = KnifeMovementChange + flagCarryingSpeed;
            }
            else
            {
                return;
            }

            if (updatedMovement == Player.Player.movement.pluginSpeedMultiplier)
            {
                return;
            }

            MovementChanger.Stop();
            MovementChanger = Plugin.Instance.StartCoroutine(ChangeMovement(updatedMovement));
        }

        public IEnumerator ChangeMovement(float newMovement)
        {
            Type type = typeof(PlayerMovement);
            FieldInfo info = type.GetField("SendPluginSpeedMultiplier", BindingFlags.NonPublic | BindingFlags.Static);
            object value = info.GetValue(null);
            ((ClientInstanceMethod<float>)value).Invoke(Player.Player.movement.GetNetId(), ENetReliability.Reliable, Player.Player.channel.GetOwnerTransportConnection(), newMovement);
            yield return new WaitForSeconds(Player.Ping - 0.01f);
            Player.Player.movement.pluginSpeedMultiplier = newMovement;
        }

        // Killstreak

        public void SetupKillstreaks()
        {
            HasKillstreakActive = false;
            ActiveKillstreak = null;

            AvailableKillstreaks = new();
            KillstreakTriggers = new();
            OrderedKillstreaks = new();

            ExtraKillstreak = ActiveLoadout.PerksSearchByType.TryGetValue("expert", out LoadoutPerk expertPerk) ? expertPerk.Perk.SkillLevel : 0;

            foreach (LoadoutKillstreak killstreak in ActiveLoadout.Killstreaks.OrderBy(k => k.Killstreak.KillstreakRequired))
            {
                OrderedKillstreaks.Add(killstreak);
                AvailableKillstreaks.Add(killstreak, false);
                KillstreakTriggers.Add(killstreak.Killstreak.KillstreakInfo.TriggerItemID, killstreak);
            }

            if (OrderedKillstreaks.Count == 0)
            {
                Plugin.Instance.UI.ClearKillstreakUI(this);
            }
            else
            {
                Plugin.Instance.UI.SetupKillstreakUI(this);
            }

            if (ExtraKillstreak > 0)
            {
                Plugin.Instance.UI.UpdateKillstreakBars(this, ExtraKillstreak);
            }
        }

        public void UpdateKillstreak(int currentKillstreak)
        {
            int updatedKillstreak = currentKillstreak + ExtraKillstreak;
            Logging.Debug($"Updating killstreak for {Player.CharacterName} with killstreak {updatedKillstreak}");
            LoadoutKillstreak availableKillstreak = OrderedKillstreaks.FirstOrDefault(k => k.Killstreak.KillstreakRequired == updatedKillstreak && !AvailableKillstreaks[k]);
            if (availableKillstreak != null)
            {
                AvailableKillstreaks[availableKillstreak] = true;
                Plugin.Instance.UI.UpdateKillstreakReady(this, availableKillstreak);
                Plugin.Instance.UI.SendAnimation(this, new AnimationInfo(EAnimationType.KillstreakAvailable, availableKillstreak.Killstreak));
            }

            Plugin.Instance.UI.UpdateKillstreakBars(this, updatedKillstreak);
        }

        public void ActivateKillstreak(LoadoutKillstreak killstreak)
        {
            Logging.Debug($"Activating killstreak with id {killstreak.Killstreak.KillstreakID} for {Player.CharacterName}");
            Data.KillstreakData info = killstreak.Killstreak.KillstreakInfo;
            PlayerInventory inv = Player.Player.inventory;
            if (info.IsItem == false) return;

            if (info.MagAmount > 0)
            {
                for (int i = 1; i <= info.MagAmount; i++)
                {
                    inv.forceAddItem(new Item(info.MagID, true), false);
                }
            }

            inv.forceAddItem(new Item(info.ItemID, true), false);
            for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
            {
                bool shouldBreak = false;
                for (int index = inv.getItemCount(page) - 1; index >= 0; index--)
                {
                    ItemJar item = inv.getItem(page, (byte)index);
                    if ((item?.item?.id ?? 0) == info.ItemID)
                    {
                        KillstreakPage = page;
                        KillstreakX = item.x;
                        KillstreakY = item.y;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) break;
            }

            Player.Player.equipment.ServerEquip(KillstreakPage, KillstreakX, KillstreakY);
            KillstreakChecker.Stop();

            HasKillstreakActive = true;
            ActiveKillstreak = killstreak;
            AvailableKillstreaks[killstreak] = false;
            Plugin.Instance.UI.UpdateKillstreakReady(this, killstreak);

            MovementChanger.Stop();
            MovementChanger = Plugin.Instance.StartCoroutine(ChangeMovement(info.MovementMultiplier));

            if (info.ItemStaySeconds == 0) return;
            Plugin.Instance.UI.SendKillstreakTimer(this, info.ItemStaySeconds);
            KillstreakChecker = Plugin.Instance.StartCoroutine(CheckKillstreak(info.ItemStaySeconds));
        }

        public void RemoveActiveKillstreak()
        {
            Logging.Debug($"Removing killstreak for {Player.CharacterName} with id {ActiveKillstreak.Killstreak.KillstreakID}");
            if (!HasKillstreakActive)
            {
                Logging.Debug($"{Player.CharacterName} has no active killstreak, what we tryna remove");
                return;
            }

            KillstreakChecker.Stop();
            KillstreakItemRemover.Stop();
            KillstreakItemRemover = Plugin.Instance.StartCoroutine(RemoveItemKillstreak(KillstreakPage, KillstreakX, KillstreakY, ActiveKillstreak.Killstreak.KillstreakInfo.MagID));

            Plugin.Instance.UI.ClearKillstreakTimer(this);
            HasKillstreakActive = false;
            ActiveKillstreak = null;
        }

        public IEnumerator RemoveItemKillstreak(byte page, byte x, byte y, ushort magID)
        {
            while (true)
            {
                if (Player.Player.life.isDead)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                PlayerInventory inv = Player.Player.inventory;
                inv.removeItem(page, Player.Player.inventory.getIndex(page, x, y));

                if (magID != 0)
                {
                    int itemCount = inv.items[2].items.Count;
                    for (int i = itemCount - 1; i >= 0; i--)
                    {
                        ItemJar item = inv.getItem(2, (byte)i);
                        if ((item?.item?.id ?? 0) == magID)
                        {
                            inv.removeItem(2, (byte)i);
                        }
                    }
                }
                break;
            }
        }

        public IEnumerator CheckKillstreak(int seconds)
        {
            for (int i = seconds; i > 0; i--)
            {
                Plugin.Instance.UI.UpdateKillstreakTimer(this, i);
                yield return new WaitForSeconds(1f);
            }
            RemoveActiveKillstreak();
        }

        // Events

        public void OnGameJoined(Game game)
        {
            CurrentGame = game;
            IsLoading = true;
        }

        public void OnGameLeft()
        {
            CurrentGame = null;
            IsLoading = false;

            TacticalChecker.Stop();
            LethalChecker.Stop();
            SpawnProtectionRemover.Stop();
            Healer.Stop();
            DamageChecker.Stop();
            RespawnTimer.Stop();
            VoiceChatChecker.Stop();
            AnimationChecker.Stop();
            MovementChanger.Stop();
            GadgetGiver.Stop();
            KillstreakItemRemover.Stop();

            HasScoreboard = false;
            HasAnimationGoingOn = false;
            IsPendingLoadoutChange = false;
            LastKiller = CSteamID.Nil;
            LastDamager.Clear();
            PendingAnimations.Clear();
            Plugin.Instance.UI.ClearDeathUI(this);
        }
    }
}
