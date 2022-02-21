﻿using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections;
using UnityEngine;
using UnturnedBlackout.GameTypes;

namespace UnturnedBlackout.Managers
{
    public class HUDManager
    {
        public const ushort ID = 27631;
        public const short Key = 27631;

        public HUDManager()
        {
            UseableGun.onChangeMagazineRequested += OnMagazineChanged;
            UseableGun.onBulletSpawned += OnBulletShot;

            PlayerEquipment.OnUseableChanged_Global += OnDequip;

            U.Events.OnPlayerConnected += OnConnected;
            U.Events.OnPlayerDisconnected += OnDisconnected;
        }

        private void OnConnected(UnturnedPlayer player)
        {
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowLifeMeters);
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowStatusIcons);
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowUseableGunStatus);
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowDeathMenu);

            player.Player.equipment.onEquipRequested += OnEquip;
            player.Player.inventory.onDropItemRequested += OnDropItem;
            player.Player.stance.onStanceUpdated += () => OnStanceUpdated(player.Player);
            EffectManager.sendUIEffect(ID, Key, player.Player.channel.GetOwnerTransportConnection(), true);

            // SOUND UI
            EffectManager.sendUIEffect(27634, 27634, player.Player.channel.GetOwnerTransportConnection(), true);
        }

        private void OnStanceUpdated(Player player)
        {
            if (Plugin.Instance.GameManager.TryGetCurrentGame(player.channel.owner.playerID.steamID, out Game game))
            {
                game.OnStanceChanged(player.stance);
            }
        }

        private void OnDisconnected(UnturnedPlayer player)
        {
            player.Player.equipment.onEquipRequested -= OnEquip;
            player.Player.inventory.onDropItemRequested -= OnDropItem;
            player.Player.stance.onStanceUpdated -= () => OnStanceUpdated(player.Player);
        }

        private void OnDropItem(PlayerInventory inventory, Item item, ref bool shouldAllow)
        {
            if (!UnturnedPlayer.FromPlayer(inventory.player).IsAdmin)
            {
                shouldAllow = false;
            }
        }

        public void OnXPChanged(UnturnedPlayer player)
        {
            Plugin.Instance.UIManager.OnXPChanged(player);
        }

        protected void OnEquip(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            var player = Plugin.Instance.GameManager.GetGamePlayer(equipment.player);

            if (Plugin.Instance.GameManager.TryGetCurrentGame(player.SteamID, out Game game) && game.GameMode == Enums.EGameType.CTF)
            {
                if (game.IsPlayerCarryingFlag(player))
                {
                    if (equipment.player.inventory.getItem(0, 0) == jar)
                    {
                        shouldAllow = false;
                        return;
                    }
                }
            }

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var connection = equipment.player.channel.GetOwnerTransportConnection();
                EffectManager.sendUIEffectVisibility(Key, connection, true, "RightSide", true);
                if (asset == null)
                {
                    return;
                }

                if (asset.type == EItemType.GUN)
                {
                    var gAsset = asset as ItemGunAsset;
                    var mAsset = Assets.find(EAssetType.ITEM, BitConverter.ToUInt16(equipment.state, 8)) as ItemMagazineAsset;
                    int currentAmmo = equipment.state[10];

                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponName", gAsset.itemName);
                    EffectManager.sendUIEffectText(Key, connection, true, "AmmoNum", currentAmmo.ToString());
                    EffectManager.sendUIEffectText(Key, connection, true, "ReserveNum", $" / {mAsset.amount}");
                }
                else
                {
                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponName", asset.itemName);
                    EffectManager.sendUIEffectText(Key, connection, true, "AmmoNum", "1");
                    EffectManager.sendUIEffectText(Key, connection, true, "ReserveNum", " / 0");
                }

                if (player != null)
                {
                    player.LastEquippedPage = equipment.equippedPage;
                    player.LastEquippedX = equipment.equipped_x;
                    player.LastEquippedY = equipment.equipped_y;
                }
            });
        }

        public void OnDequip(PlayerEquipment obj)
        {
            Plugin.Instance.StartCoroutine(EquipKnife(obj));
        }

        public void ClearGunUI(Player player)
        {
            EffectManager.sendUIEffectVisibility(Key, player.channel.GetOwnerTransportConnection(), true, "RightSide", false);
        }

        public IEnumerator EquipKnife(PlayerEquipment obj)
        {
            yield return new WaitForSeconds(0.2f);
            if (obj.useable == null && obj.canEquip && Plugin.Instance.GameManager.TryGetCurrentGame(obj.player.channel.owner.playerID.steamID, out _))
            {
                EffectManager.sendUIEffectVisibility(Key, obj.player.channel.GetOwnerTransportConnection(), true, "RightSide", false);
                var inv = obj.player.inventory;
                for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
                {
                    for (int index = inv.getItemCount(page) - 1; index >= 0; index--)
                    {
                        var item = inv.getItem(page, (byte)index);
                        if (item != null && item.item.id == Plugin.Instance.Configuration.Instance.KnifeID)
                        {
                            var asset = Assets.find(EAssetType.ITEM, item.item.id) as ItemAsset;
                            TaskDispatcher.QueueOnMainThread(() => obj.tryEquip(page, item.x, item.y));
                            yield break;
                        }
                    }
                }
            }
        }

        private void OnMagazineChanged(PlayerEquipment equipment, UseableGun gun, Item oldItem, ItemJar newItem, ref bool shouldAllow)
        {
            var amount = newItem == null ? 0 : newItem.item.amount;
            var transportConnection = equipment.player.channel.GetOwnerTransportConnection();

            EffectManager.sendUIEffectText(Key, transportConnection, true, "AmmoNum", amount.ToString());
            EffectManager.sendUIEffectText(Key, transportConnection, true, "ReserveNum", $" / {amount}");
        }

        private void OnBulletShot(UseableGun gun, BulletInfo bullet)
        {
            EffectManager.sendUIEffectText(Key, gun.player.channel.GetOwnerTransportConnection(), true, "AmmoNum", gun.player.equipment.state[10].ToString());
        }

        public void Destroy()
        {
            UseableGun.onChangeMagazineRequested -= OnMagazineChanged;
            UseableGun.onBulletSpawned -= OnBulletShot;

            U.Events.OnPlayerConnected -= OnConnected;
            U.Events.OnPlayerDisconnected -= OnDisconnected;

            PlayerEquipment.OnUseableChanged_Global -= OnDequip;
        }
    }
}
