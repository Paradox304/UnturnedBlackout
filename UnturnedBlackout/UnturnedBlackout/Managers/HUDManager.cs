using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections;
using UnityEngine;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Models.Global;

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
            var transportConnection = player.Player.channel.GetOwnerTransportConnection();

            EffectManager.sendUIEffect(ID, Key, transportConnection, true);
            RemoveGunUI(transportConnection);

            // SOUND UI
            EffectManager.sendUIEffect(27634, 27634, transportConnection, true);
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
            shouldAllow = false;
        }

        public void UpdateGadgetUI(GamePlayer player)
        {
            EffectManager.sendUIEffectImageURL(Key, player.TransportConnection, true, "TacticalIcon", "https://cdn.discordapp.com/attachments/957636187114336257/958012815870930964/smoke_grenade.png");
            EffectManager.sendUIEffectImageURL(Key, player.TransportConnection, true, "LethalIcon", "https://cdn.discordapp.com/attachments/957636187114336257/958012816470708284/grenade.png");
        }

        public void UpdateGadget(GamePlayer player, bool isTactical, bool isUsed)
        {
            Logging.Debug($"{player.Player.CharacterName}, is tactical {isTactical}, is used {isUsed}");
            EffectManager.sendUIEffectVisibility(Key, player.TransportConnection, true, $"{(isTactical ? "Tactical" : "Lethal")} Used Toggler", isUsed);
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

            if (player.ActiveLoadout == null)
            {
                return;
            }

            if (jar.item.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0) && !player.HasTactical)
            {
                shouldAllow = false;
                return;
            }
            else if (jar.item.id == (player.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0) && !player.HasLethal)
            {
                shouldAllow = false;
                return;
            }

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var connection = player.TransportConnection;
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
                    player.ForceEquip = false;
                }
                else if (asset.type == EItemType.MELEE)
                {
                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponName", asset.itemName);
                    EffectManager.sendUIEffectText(Key, connection, true, "AmmoNum", " ");
                    EffectManager.sendUIEffectText(Key, connection, true, "ReserveNum", " ");
                    player.ForceEquip = false;
                }
                else
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

                if (obj == null)
                {
                    ClearGunUI(player.TransportConnection);
                }

                if (obj.useable == null && player.ForceEquip)
                {
                    Plugin.Instance.StartCoroutine(Equip(player.Player.Player.equipment, player.LastEquippedPage, player.LastEquippedX, player.LastEquippedY));
                }
                else if (obj.useable == null && !player.ForceEquip)
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

        public void ClearGunUI(ITransportConnection transportConnection)
        {
            EffectManager.sendUIEffectText(Key, transportConnection, true, "WeaponName", "");
            EffectManager.sendUIEffectText(Key, transportConnection, true, "AmmoNum", " ");
            EffectManager.sendUIEffectText(Key, transportConnection, true, "ReserveNum", " ");
        }

        public void RemoveGunUI(ITransportConnection transportConnection)
        {
            EffectManager.sendUIEffectText(Key, transportConnection, true, "WeaponName", "");
            EffectManager.sendUIEffectText(Key, transportConnection, true, "AmmoNum", " ");
            EffectManager.sendUIEffectText(Key, transportConnection, true, "ReserveNum", " ");
            EffectManager.sendUIEffectImageURL(Key, transportConnection, true, "TacticalIcon", "");
            EffectManager.sendUIEffectImageURL(Key, transportConnection, true, "LethalIcon", "");
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
