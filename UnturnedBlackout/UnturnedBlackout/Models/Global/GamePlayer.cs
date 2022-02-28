using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.GameTypes;

namespace UnturnedBlackout.Models.Global
{
    public class GamePlayer
    {
        public CSteamID SteamID { get; set; }
        public UnturnedPlayer Player { get; set; }
        public ITransportConnection TransportConnection { get; set; }

        public bool HasScoreboard { get; set; }
        public bool HasSpawnProtection { get; set; }
        public Stack<CSteamID> LastDamager { get; set; }

        public byte LastEquippedPage { get; set; }
        public byte LastEquippedX { get; set; }
        public byte LastEquippedY { get; set; }

        public EPlayerStance PreviousStance { get; set; }

        public Coroutine Healer { get; set; }
        public Coroutine RespawnTimer { get; set; }
        public Coroutine VoiceChatChecker { get; set; }
        public Timer m_RemoveSpawnProtection { get; set; }
        public Timer m_DamageChecker { get; set; }

        public GamePlayer(UnturnedPlayer player, ITransportConnection transportConnection)
        {
            SteamID = player.CSteamID;
            Player = player;
            TransportConnection = transportConnection;
            PreviousStance = EPlayerStance.STAND;
            LastDamager = new Stack<CSteamID>(100);

            m_RemoveSpawnProtection = new Timer(1 * 1000);
            m_RemoveSpawnProtection.Elapsed += RemoveSpawnProtection;

            m_DamageChecker = new Timer(Plugin.Instance.Configuration.Instance.LastDamageAfterHealSeconds * 1000);
            m_DamageChecker.Elapsed += CheckDamage;
        }

        // Spawn Protection Seconds
        public void GiveSpawnProtection(int seconds)
        {
            HasSpawnProtection = true;
            if (m_RemoveSpawnProtection.Enabled)
            {
                m_RemoveSpawnProtection.Stop();
            }
            m_RemoveSpawnProtection.Interval = seconds * 1000;
            m_RemoveSpawnProtection.Start();
        }

        private void RemoveSpawnProtection(object sender, ElapsedEventArgs e)
        {
            HasSpawnProtection = false;
            m_RemoveSpawnProtection.Stop();
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

            if (LastDamager.Count > 0)
            {
                if (LastDamager.Peek() != damager)
                {
                    LastDamager.Push(damager);
                }
            }
            else
            {
                LastDamager.Push(damager);
            }
            m_DamageChecker.Start();
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
            var seconds = Plugin.Instance.Configuration.Instance.HealSeconds;
            while (true)
            {
                yield return new WaitForSeconds(seconds);
                var health = Player.Player.life.health;
                if (health == 100)
                {
                    LastDamager.Clear();
                    break;
                }
                Player.Player.life.serverModifyHealth(Plugin.Instance.Configuration.Instance.HealAmount);
            }
        }

        // Death screen
        public void OnDeath(CSteamID killer, int respawnSeconds)
        {
            if (!Plugin.Instance.DBManager.PlayerData.TryGetValue(killer, out PlayerData killerData))
            {
                TaskDispatcher.QueueOnMainThread(() => Player.Player.life.ServerRespawn(false));
                return;
            }
            if (m_DamageChecker.Enabled)
            {
                m_DamageChecker.Stop();
            }
            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }

            Plugin.Instance.UIManager.SendDeathUI(this, killerData);
            PreviousStance = EPlayerStance.STAND;
            RespawnTimer = Plugin.Instance.StartCoroutine(RespawnTime(respawnSeconds));
        }

        public IEnumerator RespawnTime(int respawnSeconds)
        {
            for (int seconds = respawnSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                Plugin.Instance.UIManager.UpdateRespawnTimer(this, $"{seconds}s");
            }
            Player.Player.life.ServerRespawn(false);
        }

        // Equipping and refilling on guns on respawn
        public void OnRevived()
        {
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

            Player.Player.equipment.tryEquip(LastEquippedPage, LastEquippedX, LastEquippedY);
            Plugin.Instance.UIManager.ClearDeathUI(this);
        }


        // Stance changing

        public void OnStanceChanged(EPlayerStance newStance)
        {
            TaskDispatcher.QueueOnMainThread(() =>
            {
                if (PreviousStance == EPlayerStance.CLIMB && newStance != EPlayerStance.CLIMB)
                {
                    Player.Player.equipment.tryEquip(LastEquippedPage, LastEquippedX, LastEquippedY);
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
                if (Plugin.Instance.GameManager.TryGetCurrentGame(SteamID, out Game game))
                {
                    game.OnStartedTalking(this);
                }
            }

            VoiceChatChecker = Plugin.Instance.StartCoroutine(CheckVoiceChat());
        }

        public IEnumerator CheckVoiceChat()
        {
            yield return new WaitForSeconds(1f);
            if (Plugin.Instance.GameManager.TryGetCurrentGame(SteamID, out Game game) && !Player.Player.voice.isTalking)
            {
                game.OnStoppedTalking(this);
            }

            VoiceChatChecker = null;
        }

        public void OnGameLeft()
        {
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

            HasScoreboard = false;
            LastDamager.Clear();
            Plugin.Instance.UIManager.ClearDeathUI(this);
        }
    }
}
