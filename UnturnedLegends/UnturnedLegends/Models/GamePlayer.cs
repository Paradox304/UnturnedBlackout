using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using UnityEngine;

namespace UnturnedLegends.Models
{
    public class GamePlayer
    {
        public CSteamID SteamID { get; set; }
        public UnturnedPlayer Player { get; set; }
        public ITransportConnection TransportConnection { get; set; }


        public bool HasSpawnProtection { get; set; }

        public DateTime LastDamage { get; set; }

        public Coroutine ProtectionRemover { get; set; }
        public Coroutine DamageChecker { get; set; }
        public Coroutine Healer { get; set; }

        public GamePlayer(UnturnedPlayer player, ITransportConnection transportConnection)
        {
            SteamID = player.CSteamID;
            Player = player;
            TransportConnection = transportConnection;
        }

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

        public void OnDamaged()
        {
            Utility.Debug($"{Player.CharacterName} got damaged, setting the last damage to now and checking after some seconds to heal");
            LastDamage = DateTime.UtcNow;
            if (DamageChecker != null)
            {
                Plugin.Instance.StopCoroutine(DamageChecker);
            }
            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }

            DamageChecker = Plugin.Instance.StartCoroutine(CheckDamage());
        }

        public IEnumerator CheckDamage()
        {
            yield return new WaitForSeconds(Plugin.Instance.Configuration.Instance.LastDamageAfterHealSeconds);
            Utility.Debug($"Starting healing on {Player.CharacterName}");
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
                    break;
                }
                Player.Player.life.serverModifyHealth(1);
            }
        }

        public void OnRevived()
        {
            var item = Player.Player.inventory.getItem(0, 0);
            byte page = 0;
            if (item == null)
            {
                item = Player.Player.inventory.getItem(1, 0);
                page = 1;
            }
            if (item != null)
            {
                var magID = BitConverter.ToUInt16(item.item.state, 8);
                if (Assets.find(EAssetType.ITEM, magID) is ItemMagazineAsset mAsset)
                {
                    item.item.state[10] = mAsset.amount;
                }
                Player.Player.equipment.tryEquip(page, item.x, item.y);
            }
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
        }
    }
}
