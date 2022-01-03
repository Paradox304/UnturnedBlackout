using HarmonyLib;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace UnturnedLegends.Patches
{
    [HarmonyPatch(typeof(UseableGun), "ReceiveChangeFiremode")]
    public static class OnAskFireMode_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(EFiremode newFiremode, UseableGun __instance)
        {
            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(__instance.player.channel.owner.playerID.steamID);

            Plugin.Instance.HUDManager.ChangeFiremode(player, player.Player.equipment.state[11]);
        }
    }
}
