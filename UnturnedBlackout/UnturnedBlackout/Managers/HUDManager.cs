using Rocket.Core.Utils;
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
                var connection = player.TransportConnection;
                EffectManager.sendUIEffectVisibility(Key, connection, true, "RightSide", true);
                if (asset == null)
                {
                    return;
                }

                if (asset.type == EItemType.GUN)
                {
                    var gAsset = asset as ItemGunAsset;
                    int currentAmmo = equipment.state[10];
                    var ammo = 0;
                    if (Assets.find(EAssetType.ITEM, BitConverter.ToUInt16(equipment.state, 8)) is ItemMagazineAsset mAsset)
                    {
                        ammo = mAsset.amount;
                    }

                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponName", gAsset.itemName);
                    EffectManager.sendUIEffectText(Key, connection, true, "AmmoNum", currentAmmo.ToString());
                    EffectManager.sendUIEffectText(Key, connection, true, "ReserveNum", $" / {ammo}");
                }
                else if (asset.type == EItemType.MELEE)
                {
                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponName", asset.itemName);
                    EffectManager.sendUIEffectText(Key, connection, true, "AmmoNum", " ");
                    EffectManager.sendUIEffectText(Key, connection, true, "ReserveNum", " ");
                } else
                {
                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponName", asset.itemName);
                    EffectManager.sendUIEffectText(Key, connection, true, "AmmoNum", " ");
                    EffectManager.sendUIEffectText(Key, connection, true, "ReserveNum", " ");
                    player.ForceEquip = true;
                    return;
                }

                player.LastEquippedPage = equipment.equippedPage;
                player.LastEquippedX = equipment.equipped_x;
                player.LastEquippedY = equipment.equipped_y;
            });
        }

        public void OnDequip(PlayerEquipment obj)
        {
            var player = Plugin.Instance.GameManager.GetGamePlayer(obj.player);
            if (player != null)
            {
                if (!Plugin.Instance.GameManager.TryGetCurrentGame(player.SteamID, out _))
                {
                    return;
                }

                if (player.ActiveLoadout == null)
                {
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, player.TransportConnection, true, "RightSide", obj != null);
                if (obj.useable == null && player.ForceEquip)
                {
                    Plugin.Instance.StartCoroutine(Equip(player.Player.Player.equipment, player.LastEquippedPage, player.LastEquippedX, player.LastEquippedY));
                } else if (obj.useable == null && !player.ForceEquip)
                {
                    var inv = player.Player.Player.inventory;
                    for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
                    {
                        for (int index = inv.getItemCount(page) - 1; index >= 0; index--)
                        {
                            var item = inv.getItem(page, (byte)index);
                            if (item != null && item.item.id == (player.ActiveLoadout.Knife?.Knife?.KnifeID ?? 0))
                            {
                                Plugin.Instance.StartCoroutine(Equip(player.Player.Player.equipment, page, item.x, item.y));
                                break;
                            }
                        }
                    }
                }
            }
        }

        public IEnumerator Equip(PlayerEquipment equipment, byte page, byte x, byte y)
        {
            yield return new WaitForSeconds(0.2f);
            if (equipment.useable == null && equipment.canEquip)
            {
                equipment.tryEquip(page, x, y);
            }
        }

        public void ClearGunUI(Player player)
        {
            EffectManager.sendUIEffectVisibility(Key, player.channel.GetOwnerTransportConnection(), true, "RightSide", false);
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
