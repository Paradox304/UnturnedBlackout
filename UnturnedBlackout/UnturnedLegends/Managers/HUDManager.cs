﻿using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using UnityEngine;
using UnturnedBlackout.Database;

namespace UnturnedBlackout.Managers
{
    public class HUDManager
    {
        public const ushort ID = 27631;
        public const short Key = 27631;

        public HUDManager()
        {
            UnturnedPlayerEvents.OnPlayerUpdateHealth += OnHealthChanged;
            UnturnedPlayerEvents.OnPlayerUpdateStamina += OnStaminaChanged;

            UnturnedPlayerEvents.OnPlayerRevive += OnRevived;

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

            EffectManager.sendUIEffect(ID, Key, player.Player.channel.GetOwnerTransportConnection(), true);
            EffectManager.sendUIEffect(27634, 27634, player.Player.channel.GetOwnerTransportConnection(), true);

            OnHealthChanged(player, player.Player.life.health);
        }

        private void OnDisconnected(UnturnedPlayer player)
        {
            player.Player.equipment.onEquipRequested -= OnEquip;
        }

        public void ShowHUD(UnturnedPlayer player)
        {
            EffectManager.sendUIEffectVisibility(Key, player.Player.channel.GetOwnerTransportConnection(), true, "LeftSide", true);
        }

        public void HideHUD(UnturnedPlayer player)
        {
            EffectManager.sendUIEffectVisibility(Key, player.Player.channel.GetOwnerTransportConnection(), true, "LeftSide", false);
        }

        private void OnRevived(UnturnedPlayer player, Vector3 position, byte angle)
        {
            OnHealthChanged(player, player.Player.life.health);
        }

        private void OnHealthChanged(UnturnedPlayer player, byte health)
        {
            var spaces = health * 96 / 100;
            var transportConnection = player.Player.channel.GetOwnerTransportConnection();

            EffectManager.sendUIEffectText(Key, transportConnection, true, "HealthBarFill", spaces == 0 ? " " : new string(' ', spaces));
            EffectManager.sendUIEffectText(Key, transportConnection, true, "HealthNum", health.ToString());
        }

        public void OnXPChanged(UnturnedPlayer player)
        {
            var transportConnection = player.Player.channel.GetOwnerTransportConnection();
            var xp = (uint)0;
            var level = (uint)0;
            var neededXP = 0;
            if (Plugin.Instance.DBManager.PlayerCache.TryGetValue(player.CSteamID, out PlayerData data))
            {
                xp = data.XP;
                level = data.Level;
                neededXP = data.GetNeededXP();
            }

            var spaces = neededXP == 0 ? 0 : (int)(xp * 96 / neededXP);
            EffectManager.sendUIEffectText(Key, transportConnection, true, "XPNum", Plugin.Instance.Translate("Level_Show", level).ToRich());
            EffectManager.sendUIEffectText(Key, transportConnection, true, "XPBarFill", spaces == 0 ? " " : new string(' ', spaces));

            Plugin.Instance.UIManager.OnXPChanged(player);
        }

        private void OnEquip(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            TaskDispatcher.QueueOnMainThread(() =>
            {
                var connection = equipment.player.channel.GetOwnerTransportConnection();
                EffectManager.sendUIEffectVisibility(Key, connection, true, "RightSide", true);
                if (asset.type == EItemType.GUN)
                {
                    var gAsset = asset as ItemGunAsset;
                    var mAsset = Assets.find(EAssetType.ITEM, BitConverter.ToUInt16(equipment.state, 8)) as ItemMagazineAsset;
                    string firemode = GetFiremode(equipment.state[11]);
                    int currentAmmo = equipment.state[10];

                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponName", gAsset.itemName);
                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponMode", firemode);
                    EffectManager.sendUIEffectText(Key, connection, true, "AmmoNum", currentAmmo.ToString());
                    EffectManager.sendUIEffectText(Key, connection, true, "ReserveNum", $" / {mAsset.amount}");
                }
                else
                {
                    EffectManager.sendUIEffectText(Key, connection, true, "WeaponName", asset.itemName);
                    EffectManager.sendUIEffectText(Key, connection, true, "AmmoNum", "1");
                    EffectManager.sendUIEffectText(Key, connection, true, "ReserveNum", " / 0");
                }
            });
        }

        private void OnDequip(PlayerEquipment obj)
        {
            if (obj.useable == null)
            {
                EffectManager.sendUIEffectVisibility(Key, obj.player.channel.GetOwnerTransportConnection(), true, "RightSide", false);
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

        private void OnStaminaChanged(UnturnedPlayer player, byte stamina)
        {
            if (stamina <= 20)
            {
                player.Player.life.serverModifyStamina(100);
            }
        }

        public void ChangeFiremode(UnturnedPlayer player, byte newFiremode)
        {
            EffectManager.sendUIEffectText(Key, player.SteamPlayer().transportConnection, true, "WeaponMode", GetFiremode(newFiremode));
        }

        public string GetFiremode(byte firemode)
        {
            switch (firemode)
            {
                case (byte)EFiremode.SAFETY:
                    return Plugin.Instance.Translate("SAFETY");
                case (byte)EFiremode.SEMI:
                    return Plugin.Instance.Translate("SEMI");
                case (byte)EFiremode.BURST:
                    return Plugin.Instance.Translate("BURST");
                case (byte)EFiremode.AUTO:
                    return Plugin.Instance.Translate("AUTO");
                default:
                    return "None";
            }
        }

        public void Destroy()
        {
            UnturnedPlayerEvents.OnPlayerUpdateHealth -= OnHealthChanged;
            UnturnedPlayerEvents.OnPlayerUpdateStamina -= OnStaminaChanged;

            UnturnedPlayerEvents.OnPlayerRevive -= OnRevived;

            UseableGun.onChangeMagazineRequested -= OnMagazineChanged;
            UseableGun.onBulletSpawned -= OnBulletShot;

            PlayerEquipment.OnUseableChanged_Global -= OnDequip;

            U.Events.OnPlayerConnected -= OnConnected;
            U.Events.OnPlayerDisconnected -= OnDisconnected;
        }
    }
}