using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using UnturnedLegends.Database;
using UnturnedLegends.Enums;
using UnturnedLegends.Models;

namespace UnturnedLegends.Managers
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

            U.Events.OnPlayerConnected += OnConnected;
        }

        private void OnConnected(UnturnedPlayer player)
        {
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowLifeMeters);
            player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowStatusIcons);

            EffectManager.sendUIEffect(ID, Key, player.Player.channel.GetOwnerTransportConnection(), true);

            OnHealthChanged(player, player.Player.life.health);
            OnGamemodeChanged(player.Player, Plugin.Instance.GameManager.CurrentLocation, Plugin.Instance.GameManager.CurrentGame);
        }

        private void OnRevived(UnturnedPlayer player, Vector3 position, byte angle)
        {
            OnHealthChanged(player, player.Player.life.health);
        }

        private void OnHealthChanged(UnturnedPlayer player, byte health)
        {
            var spaces = health * 96 / 100;
            var transportConnection = player.Player.channel.GetOwnerTransportConnection();

            EffectManager.sendUIEffectText(Key, transportConnection, true, "HealthBarFill", new string(' ', spaces));
            EffectManager.sendUIEffectText(Key, transportConnection, true, "HealthNum", health.ToString());
        }

        public void OnXPChanged(UnturnedPlayer player)
        {
            var xp = Plugin.Instance.DBManager.PlayerCache.TryGetValue(player.CSteamID, out PlayerData data) ? data.XP : 0;

            EffectManager.sendUIEffectText(Key, player.Player.channel.GetOwnerTransportConnection(), true, "XPNum", $"{xp} XP");
        }

        public void OnGamemodeChanged(Player player, ArenaLocation location, EGameType gameType)
        {
            var transportConnection = player.channel.GetOwnerTransportConnection();

            EffectManager.sendUIEffectText(Key, transportConnection, true, "GamemodeName", gameType == EGameType.None ? "None" : Plugin.Instance.Translate($"{gameType}_Name").ToRich());
            EffectManager.sendUIEffectText(Key, transportConnection, true, "ArenaName", location.LocationID == -1 ? "None" : Plugin.Instance.Translate("Arena_Name", location.LocationName).ToRich());
        }

        private void OnMagazineChanged(PlayerEquipment equipment, UseableGun gun, Item oldItem, ItemJar newItem, ref bool shouldAllow)
        {
            // Not implemented yet
        }

        private void OnBulletShot(UseableGun gun, BulletInfo bullet)
        {
            // Not implemented yet
        }

        private void OnStaminaChanged(UnturnedPlayer player, byte stamina)
        {
            if (stamina <= 20)
            {
                player.Player.life.serverModifyStamina(100);
            }
        }

        public void Destroy()
        {
            UnturnedPlayerEvents.OnPlayerUpdateHealth -= OnHealthChanged;
            UnturnedPlayerEvents.OnPlayerUpdateStamina -= OnStaminaChanged;

            UnturnedPlayerEvents.OnPlayerRevive -= OnRevived;

            UseableGun.onChangeMagazineRequested -= OnMagazineChanged;
            UseableGun.onBulletSpawned -= OnBulletShot;

            U.Events.OnPlayerConnected -= OnConnected;
        }
    }
}
