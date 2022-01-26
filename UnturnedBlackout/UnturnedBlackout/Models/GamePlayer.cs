﻿using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.Database;

namespace UnturnedBlackout.Models
{
    public class GamePlayer
    {
        public CSteamID SteamID { get; set; }
        public UnturnedPlayer Player { get; set; }
        public ITransportConnection TransportConnection { get; set; }

        public bool HasSpawnProtection { get; set; }
        public Stack<CSteamID> LastDamager { get; set; }

        public Coroutine ProtectionRemover { get; set; }
        public Coroutine DamageChecker { get; set; }
        public Coroutine Healer { get; set; }
        public Coroutine RespawnTimer { get; set; }

        public GamePlayer(UnturnedPlayer player, ITransportConnection transportConnection)
        {
            SteamID = player.CSteamID;
            Player = player;
            TransportConnection = transportConnection;
            LastDamager = new Stack<CSteamID>(100);
        }

        // Spawn Protection Seconds
        public void GiveSpawnProtection(int seconds)
        {
            HasSpawnProtection = true;
            if (ProtectionRemover != null)
            {
                Plugin.Instance.StopCoroutine(ProtectionRemover);
            }
            Plugin.Instance.StartCoroutine(RemoveSpawnProtection(seconds));
        }

        public IEnumerator RemoveSpawnProtection(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            HasSpawnProtection = false;
        }

        // Healing
        public void OnDamaged(CSteamID damager)
        {
            if (DamageChecker != null)
            {
                Plugin.Instance.StopCoroutine(DamageChecker);
            }
            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }

            Utility.Debug($"{Player.CharacterName} got damaged by {damager}");
            if (LastDamager.Count > 0)
            {
                if (LastDamager.Peek() != damager)
                {
                    LastDamager.Push(damager);
                }
            } else
            {
                LastDamager.Push(damager);
            }
            DamageChecker = Plugin.Instance.StartCoroutine(CheckDamage());
        }

        public IEnumerator CheckDamage()
        {
            yield return new WaitForSeconds(Plugin.Instance.Configuration.Instance.LastDamageAfterHealSeconds);
            Healer = Plugin.Instance.StartCoroutine(HealPlayer());
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
        public void OnDeath(CSteamID killer)
        {
            if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(killer, out PlayerData killerData))
            {
                Player.Player.life.ServerRespawn(false);
                return;
            }
            if (DamageChecker != null)
            {
                Plugin.Instance.StopCoroutine(DamageChecker);
            }
            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }

            Plugin.Instance.UIManager.SendDeathUI(this, killerData);
            RespawnTimer = Plugin.Instance.StartCoroutine(RespawnTime());
        }

        public IEnumerator RespawnTime()
        {
            for (int seconds = Plugin.Instance.Configuration.Instance.RespawnSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                Plugin.Instance.UIManager.UpdateRespawnTimer(this, $"{seconds}s");
            }
            Player.Player.life.ServerRespawn(false);
        }

        // Equipping and refilling on guns on respawn
        public void OnRevived()
        {
            bool hasEquip = false;

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

                    if (!hasEquip)
                    {
                        Player.Player.equipment.tryEquip(i, item.x, item.y);
                        hasEquip = true;
                    }
                }
            }

            Plugin.Instance.UIManager.ClearDeathUI(this);
        }

        public void OnGameLeft()
        {
            if (ProtectionRemover != null)
            {
                Plugin.Instance.StopCoroutine(ProtectionRemover);
            }

            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }

            if (DamageChecker != null)
            {
                Plugin.Instance.StopCoroutine(DamageChecker);
            }

            if (RespawnTimer != null)
            {
                Plugin.Instance.StopCoroutine(RespawnTimer);
            }

            LastDamager.Clear();
            Plugin.Instance.UIManager.ClearDeathUI(this);
        }
    }
}
