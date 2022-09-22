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
using System.Timers;
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

        //public Timer m_RemoveSpawnProtection { get; set; }
        //public Timer m_DamageChecker { get; set; }
        //public Timer m_TacticalChecker { get; set; }
        //public Timer m_LethalChecker { get; set; }
        //public Timer m_KillstreakChecker { get; set; }

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

            /*
            m_RemoveSpawnProtection = new Timer(1 * 1000)
            {
                AutoReset = false
            };
            m_RemoveSpawnProtection.Elapsed += RemoveSpawnProtection;

            m_DamageChecker = new Timer(Config.Base.FileData.LastDamageAfterHealSeconds * 1000)
            {
                AutoReset = false
            };
            m_DamageChecker.Elapsed += CheckDamage;

            m_TacticalChecker = new Timer(1 * 1000)
            {
                AutoReset = false
            };
            m_TacticalChecker.Elapsed += EnableTactical;

            m_LethalChecker = new Timer(1 * 1000)
            {
                AutoReset = false
            };
            m_LethalChecker.Elapsed += EnableLethal;

            m_KillstreakChecker = new Timer(1 * 1000)
            {
                AutoReset = false
            };

            m_KillstreakChecker.Elapsed += CheckKillstreak;
            */

        }

        // Spawn Protection Seconds
        public void GiveSpawnProtection(int seconds)
        {
            /*
            if (m_RemoveSpawnProtection.Enabled)
            {
                Logging.Debug($"Timer to remove spawn protection is already enabled");
                m_RemoveSpawnProtection.Stop();
            }
            HasSpawnProtection = true;
            Logging.Debug($"Setting HasSpawnProtection to {HasSpawnProtection}");
            m_RemoveSpawnProtection.Interval = seconds * 1000;
            Logging.Debug($"Setting timer removal interval to {m_RemoveSpawnProtection.Interval}");
            m_RemoveSpawnProtection.Start();
            Logging.Debug($"Starting timer to remove spawn protection at {DateTime.UtcNow}");
            */

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

        /*
        private void RemoveSpawnProtection(object sender, ElapsedEventArgs e)
        {
            Logging.Debug($"Timer to remove spawn protection for {Player.CharacterName} has passed at {DateTime.UtcNow} removing spawn protection");
            HasSpawnProtection = false;
        }
        */

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

            /*
            if (m_LethalChecker.Enabled)
            {
                m_LethalChecker.Stop();
            }

            if (m_TacticalChecker.Enabled)
            {
                m_TacticalChecker.Stop();
            }
            */

            var medic = loadout.PerksSearchByType.TryGetValue("medic", out LoadoutPerk medicPerk) ? medicPerk.Perk.SkillLevel : 0f;
            HealAmount = Config.Base.FileData.HealAmount * (1 + (medic / 100));

            Plugin.Instance.UI.SendGadgetIcons(this);

            if (loadout.Tactical != null)
            {
                HasTactical = true;
                var tactician = loadout.PerksSearchByType.TryGetValue("tactician", out LoadoutPerk tacticianPerk) ? tacticianPerk.Perk.SkillLevel : 0f;
                TacticalIntervalSeconds = (float)loadout.Tactical.Gadget.GiveSeconds * (1 - (tactician / 100));
                //m_TacticalChecker.Interval = (float)loadout.Tactical.Gadget.GiveSeconds * 1000 * (1 - (tactician / 100));
            }

            if (loadout.Lethal != null)
            {
                HasLethal = true;
                var grenadier = loadout.PerksSearchByType.TryGetValue("grenadier", out LoadoutPerk grenadierPerk) ? grenadierPerk.Perk.SkillLevel : 0f;
                LethalIntervalSeconds = (float)loadout.Lethal.Gadget.GiveSeconds * (1 - (grenadier / 100));
                //m_LethalChecker.Interval = (float)loadout.Lethal.Gadget.GiveSeconds * 1000 * (1 - (grenadier / 100));
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
                var questConditions = new Dictionary<EQuestCondition, int>
                {
                    { EQuestCondition.Map, CurrentGame.Location.LocationID },
                    { EQuestCondition.Gamemode, (int)CurrentGame.GameMode },
                    { EQuestCondition.Gadget, ActiveLoadout.Tactical.Gadget.GadgetID }
                };
                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Quest.CheckQuest(this, EQuestType.GadgetsUsed, questConditions));
            }

            /*
            if (m_TacticalChecker.Enabled)
            {
                m_TacticalChecker.Stop();
            }
            m_TacticalChecker.Start();
            */

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
                var questConditions = new Dictionary<EQuestCondition, int>
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

        /*
        private void EnableLethal(object sender, ElapsedEventArgs e)
        {
            HasLethal = true;

            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateGadgetUsed(this, false, false));
        }

        private void EnableTactical(object sender, ElapsedEventArgs e)
        {
            HasTactical = true;

            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateGadgetUsed(this, true, false));
        }
        */

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

            var damagerPlayer = Plugin.Instance.Game.GetGamePlayer(damager);
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

        /*
        private void CheckDamage(object sender, ElapsedEventArgs e)
        {
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Healer = Plugin.Instance.StartCoroutine(HealPlayer());
            });
        }
        */

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
            var victimData = victim.Data;
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

            var killerPlayer = Plugin.Instance.Game.GetGamePlayer(killer);
            if (killer == null)
            {
                return;
            }

            Plugin.Instance.UI.SendDeathUI(this, killerPlayer, killerData);
            PreviousStance = EPlayerStance.STAND;
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
            if (IsPendingLoadoutChange)
            {
                Plugin.Instance.Loadout.GiveLoadout(this, kit, gloves);
                IsPendingLoadoutChange = false;
                Plugin.Instance.UI.ClearDeathUI(this);
                return;
            }

            for (byte i = 0; i <= 1; i++)
            {
                var item = Player.Player.inventory.getItem(i, 0);
                if (item != null && item.item.state.Length > 8)
                {
                    var magID = BitConverter.ToUInt16(item.item.state, 8);
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

            Logging.Debug($"Sending movement for {Player.CharacterName} with isADS {isADS}, isCarryingFlag {isCarryingFlag}");
            var flagCarryingSpeed = isCarryingFlag ? Config.CTF.FileData.FlagCarryingSpeed : 0f;
            float updatedMovement;
            if (isCarryingFlag)
            {
                Logging.Debug($"Sending movement is carrying flag");
                updatedMovement = Config.CTF.FileData.FlagCarryingSpeed;
            }
            else if (Player.Player.equipment.itemID == (ActiveLoadout.Primary?.Gun?.GunID ?? 0) || Player.Player.equipment.itemID == (ActiveLoadout.PrimarySkin?.SkinID ?? 0))
            {
                Logging.Debug($"Sending movement primary");
                updatedMovement = PrimaryMovementChange + SecondaryMovementChange + (isADS ? PrimaryMovementChangeADS : 0) + flagCarryingSpeed;
            }
            else if (Player.Player.equipment.itemID == (ActiveLoadout.Secondary?.Gun?.GunID ?? 0) || Player.Player.equipment.itemID == (ActiveLoadout.SecondarySkin?.SkinID ?? 0))
            {
                Logging.Debug($"Sending movement secondary");
                updatedMovement = PrimaryMovementChange + SecondaryMovementChange + (isADS ? SecondaryMovementChangeADS : 0) + flagCarryingSpeed;
            }
            else if (Player.Player.equipment.itemID == (ActiveLoadout.Knife.Knife.KnifeID))
            {
                Logging.Debug($"Sending movement knife");
                updatedMovement = KnifeMovementChange + flagCarryingSpeed;
            }
            else
            {
                Logging.Debug($"Other movement, returning");
                return;
            }

            Logging.Debug($"Updated Movement: {updatedMovement}");
            if (updatedMovement == Player.Player.movement.pluginSpeedMultiplier)
            {
                return;
            }

            MovementChanger.Stop();
            MovementChanger = Plugin.Instance.StartCoroutine(ChangeMovement(updatedMovement));
        }

        public IEnumerator ChangeMovement(float newMovement)
        {
            var type = typeof(PlayerMovement);
            var info = type.GetField("SendPluginSpeedMultiplier", BindingFlags.NonPublic | BindingFlags.Static);
            var value = info.GetValue(null);
            ((ClientInstanceMethod<float>)value).Invoke(Player.Player.movement.GetNetId(), ENetReliability.Reliable, Player.Player.channel.GetOwnerTransportConnection(), newMovement);
            yield return new WaitForSeconds(Player.Ping - 0.01f);
            Player.Player.movement.pluginSpeedMultiplier = newMovement;
        }

        // Killstreak

        public void SetupKillstreaks()
        {
            Logging.Debug($"Setting up killstreaks for {Player.CharacterName}");
            HasKillstreakActive = false;
            ActiveKillstreak = null;

            AvailableKillstreaks = new();
            KillstreakTriggers = new();
            OrderedKillstreaks = new();

            foreach (var killstreak in ActiveLoadout.Killstreaks.OrderBy(k => k.Killstreak.KillstreakRequired))
            {
                Logging.Debug($"Adding killstreak with id {killstreak.Killstreak.KillstreakID} and trigger item id {killstreak.Killstreak.KillstreakInfo.TriggerItemID} and killstreak required {killstreak.Killstreak.KillstreakRequired}");
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
        }
        
        public void UpdateKillstreak(int currentKillstreak)
        {
            Logging.Debug($"Updating killstreak for {Player.CharacterName} with current killstreak {currentKillstreak}");
            var availableKillstreak = OrderedKillstreaks.FirstOrDefault(k => k.Killstreak.KillstreakRequired == currentKillstreak && !AvailableKillstreaks[k]);
            if (availableKillstreak != null)
            {
                Logging.Debug($"Found killstreak with id {availableKillstreak.Killstreak.KillstreakID} that is to be made available with requirement {availableKillstreak.Killstreak.KillstreakRequired}");
                AvailableKillstreaks[availableKillstreak] = true;
                Plugin.Instance.UI.UpdateKillstreakReady(this, availableKillstreak);
            }

            Logging.Debug($"Updating killstreak bars");
            Plugin.Instance.UI.UpdateKillstreakBars(this, currentKillstreak);
        }

        public void ActivateKillstreak(LoadoutKillstreak killstreak)
        {
            Logging.Debug($"Activating killstreak with id {killstreak.Killstreak.KillstreakID} for {Player.CharacterName}");
            var info = killstreak.Killstreak.KillstreakInfo;
            var inv = Player.Player.inventory;
            if (info.IsItem == false) return;

            if (info.MagAmount > 0)
            {
                for (int i = 1; i <= info.MagAmount; i++)
                {
                    inv.forceAddItem(new Item(info.MagID, true), false);
                }
                Logging.Debug($"Killstreak has multiple ammo, gave {info.MagAmount} magazines with id {info.MagID}");
            }

            inv.forceAddItem(new Item(info.ItemID, true), false);
            Logging.Debug($"Gave the item to the player");
            for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
            {
                var shouldBreak = false;
                for (int index = inv.getItemCount(page) - 1; index >= 0; index--)
                {
                    var item = inv.getItem(page, (byte)index);
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
            Logging.Debug($"Stored the page: {KillstreakPage}, x: {KillstreakX}, y: {KillstreakY} for the item sent to the player");
            KillstreakChecker.Stop();

            HasKillstreakActive = true;
            ActiveKillstreak = killstreak;
            AvailableKillstreaks[killstreak] = false;
            Plugin.Instance.UI.UpdateKillstreakReady(this, killstreak);
            Logging.Debug($"Made the killstreak active on code side, sent the UI");

            if (info.ItemStaySeconds == 0) return;
            KillstreakChecker = Plugin.Instance.StartCoroutine(CheckKillstreak(info.ItemStaySeconds));
            Logging.Debug($"Item stay seconds is not 0, starting the killstreak remover timer at {DateTime.UtcNow}");
        }

        public void RemoveActiveKillstreak()
        {
            Logging.Debug($"Removing the active killstreak for {Player.CharacterName}");
            if (!HasKillstreakActive)
            {
                Logging.Debug($"{Player.CharacterName} has no active killstreak, what we tryna remove");
                return;
            }

            KillstreakChecker.Stop();
            KillstreakItemRemover.Stop();
            KillstreakItemRemover = Plugin.Instance.StartCoroutine(RemoveItemKillstreak(KillstreakPage, KillstreakX, KillstreakY, ActiveKillstreak.Killstreak.KillstreakInfo.MagID));
            Logging.Debug($"Removed the item of the killstreak for {Player.CharacterName}");

            HasKillstreakActive = false;
            ActiveKillstreak = null;
            Logging.Debug($"Completely removed the killstreak from {Player.CharacterName}");            
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

                var inv = Player.Player.inventory;
                inv.removeItem(page, Player.Player.inventory.getIndex(page, x, y));

                if (magID != 0)
                {
                    Logging.Debug("Removing killstreak, killstreak has mags for it. Removing all the mags as well");
                    var itemCount = inv.items[2].items.Count;
                    for (int i = itemCount - 1; i >= 0; i--)
                    {
                        var item = inv.getItem(2, (byte)i);
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
            yield return new WaitForSeconds(seconds);
            RemoveActiveKillstreak();
        }

        /*
        private void CheckKillstreak(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }
        */
        // Events

        public void OnGameJoined(Game game)
        {
            CurrentGame = game;
        }

        public void OnGameLeft()
        {
            CurrentGame = null;

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
