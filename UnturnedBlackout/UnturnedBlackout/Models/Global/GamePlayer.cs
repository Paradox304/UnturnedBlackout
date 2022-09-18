using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
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

        public bool HasTactical { get; set; }
        public bool HasLethal { get; set; }

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
        public Timer m_RemoveSpawnProtection { get; set; }
        public Timer m_DamageChecker { get; set; }
        public Timer m_TacticalChecker { get; set; }
        public Timer m_LethalChecker { get; set; }

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

            m_RemoveSpawnProtection = new Timer(1 * 1000);
            m_RemoveSpawnProtection.Elapsed += RemoveSpawnProtection;

            m_DamageChecker = new Timer(Config.Base.FileData.LastDamageAfterHealSeconds * 1000);
            m_DamageChecker.Elapsed += CheckDamage;

            m_TacticalChecker = new Timer(1 * 1000);
            m_TacticalChecker.Elapsed += EnableTactical;

            m_LethalChecker = new Timer(1 * 1000);
            m_LethalChecker.Elapsed += EnableLethal;
        }

        // Spawn Protection Seconds
        public void GiveSpawnProtection(int seconds)
        {
            Logging.Debug($"Giving {Player.CharacterName} spawn protection for {seconds} seconds");
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
        }

        private void RemoveSpawnProtection(object sender, ElapsedEventArgs e)
        {
            Logging.Debug($"Timer to remove spawn protection for {Player.CharacterName} has passed at {DateTime.UtcNow} removing spawn protection");
            HasSpawnProtection = false;
            m_RemoveSpawnProtection.Stop();
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

            if (m_LethalChecker.Enabled)
            {
                m_LethalChecker.Stop();
            }

            if (m_TacticalChecker.Enabled)
            {
                m_TacticalChecker.Stop();
            }

            var medic = loadout.PerksSearchByType.TryGetValue("medic", out LoadoutPerk medicPerk) ? medicPerk.Perk.SkillLevel : 0f;
            HealAmount = Config.Base.FileData.HealAmount * (1 + (medic / 100));

            Plugin.Instance.UI.SendGadgetIcons(this);

            if (loadout.Tactical != null)
            {
                HasTactical = true;
                var tactician = loadout.PerksSearchByType.TryGetValue("tactician", out LoadoutPerk tacticianPerk) ? tacticianPerk.Perk.SkillLevel : 0f;
                m_TacticalChecker.Interval = (float)loadout.Tactical.Gadget.GiveSeconds * 1000 * (1 - (tactician / 100));
            }

            if (loadout.Lethal != null)
            {
                HasLethal = true;
                var grenadier = loadout.PerksSearchByType.TryGetValue("grenadier", out LoadoutPerk grenadierPerk) ? grenadierPerk.Perk.SkillLevel : 0f;
                m_LethalChecker.Interval = (float)loadout.Lethal.Gadget.GiveSeconds * 1000 * (1 - (grenadier / 100));
            }

            Plugin.Instance.UI.UpdateGadgetUsed(this, false, !HasLethal);
            Plugin.Instance.UI.UpdateGadgetUsed(this, true, !HasTactical);
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

            if (m_TacticalChecker.Enabled)
            {
                m_TacticalChecker.Stop();
            }
            m_TacticalChecker.Start();
            Plugin.Instance.StartCoroutine(GiveGadget(ActiveLoadout.Tactical.Gadget.GadgetID));
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

            if (m_LethalChecker.Enabled)
            {
                m_LethalChecker.Stop();
            }
            m_LethalChecker.Start();
            Plugin.Instance.StartCoroutine(GiveGadget(ActiveLoadout.Lethal.Gadget.GadgetID));
        }

        private void EnableLethal(object sender, ElapsedEventArgs e)
        {
            HasLethal = true;
            m_LethalChecker.Stop();

            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateGadgetUsed(this, false, false));
        }

        private void EnableTactical(object sender, ElapsedEventArgs e)
        {
            HasTactical = true;
            m_TacticalChecker.Stop();

            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateGadgetUsed(this, true, false));
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
            if (m_DamageChecker.Enabled)
            {
                m_DamageChecker.Stop();
            }
            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }
            m_DamageChecker.Start();

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

        private void CheckDamage(object sender, ElapsedEventArgs e)
        {
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Healer = Plugin.Instance.StartCoroutine(HealPlayer());
            });
            m_DamageChecker.Stop();
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
            var victimData = victim.Data;
            Plugin.Instance.UI.SendKillCard(this, victim, victimData);
        }

        // Death screen

        public void OnDeath(CSteamID killer, int respawnSeconds)
        {
            if (!Plugin.Instance.DB.PlayerData.TryGetValue(killer, out PlayerData killerData))
            {
                TaskDispatcher.QueueOnMainThread(() => Player.Player.life.ServerRespawn(false));
                return;
            }

            if (killer != SteamID)
            {
                LastKiller = killer;
            }

            if (m_DamageChecker.Enabled)
            {
                m_DamageChecker.Stop();
            }
            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }
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
                    Player.Player.equipment.ServerEquip(LastEquippedPage, LastEquippedX, LastEquippedY);
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
            else
            {
                if (CurrentGame != null)
                {
                    CurrentGame.OnStartedTalking(this);
                }
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
                Player.Player.movement.sendPluginSpeedMultiplier(1f);
                return;
            }

            var flagCarryingSpeed = isCarryingFlag ? Config.CTF.FileData.FlagCarryingSpeed : 0f;
            var updatedMovement = 1f + flagCarryingSpeed;
            if (Player.Player.equipment.itemID == (ActiveLoadout.Primary?.Gun?.GunID ?? 0) || Player.Player.equipment.itemID == (ActiveLoadout.PrimarySkin?.SkinID ?? 0))
            {
                updatedMovement = (PrimaryMovementChange + SecondaryMovementChange) + (isADS ? PrimaryMovementChangeADS : 0) + flagCarryingSpeed;
            }
            else if (Player.Player.equipment.itemID == (ActiveLoadout.Secondary?.Gun?.GunID ?? 0) || Player.Player.equipment.itemID == (ActiveLoadout.PrimarySkin?.SkinID ?? 0))
            {
                updatedMovement = (PrimaryMovementChange + SecondaryMovementChange) + (isADS ? SecondaryMovementChangeADS : 0) + flagCarryingSpeed;
            }
            else if (Player.Player.equipment.itemID == (ActiveLoadout.Knife.Knife.KnifeID))
            {
                updatedMovement = KnifeMovementChange + flagCarryingSpeed;
            }

            if (updatedMovement == Player.Player.movement.pluginSpeedMultiplier)
            {
                return;
            }

            if (MovementChanger != null)
            {
                Plugin.Instance.StopCoroutine(MovementChanger);
            }

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

        // Events

        public void OnGameJoined(Game game)
        {
            CurrentGame = game;
        }

        public void OnGameLeft()
        {
            CurrentGame = null;

            if (m_TacticalChecker.Enabled)
            {
                m_TacticalChecker.Stop();
            }

            if (m_LethalChecker.Enabled)
            {
                m_LethalChecker.Stop();
            }

            if (m_RemoveSpawnProtection.Enabled)
            {
                m_RemoveSpawnProtection.Stop();
            }

            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }

            if (m_DamageChecker.Enabled)
            {
                m_DamageChecker.Stop();
            }

            if (RespawnTimer != null)
            {
                Plugin.Instance.StopCoroutine(RespawnTimer);
            }

            if (VoiceChatChecker != null)
            {
                Plugin.Instance.StopCoroutine(VoiceChatChecker);
            }

            if (AnimationChecker != null)
            {
                Plugin.Instance.StopCoroutine(AnimationChecker);
            }

            if (MovementChanger != null)
            {
                Plugin.Instance.StopCoroutine(MovementChanger);
            }

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
