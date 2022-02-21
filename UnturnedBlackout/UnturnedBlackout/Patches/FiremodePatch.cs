using HarmonyLib;
using SDG.Unturned;
using UnturnedBlackout.GameTypes;

namespace UnturnedBlackout.Patches
{
    [HarmonyPatch(typeof(UseableGun), "ReceiveChangeFiremode")]
    public static class FiremodePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(EFiremode newFiremode, UseableGun __instance)
        {
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(__instance.player);
            if (gPlayer == null)
            {
                return true;
            }

            if (!Plugin.Instance.GameManager.TryGetCurrentGame(gPlayer.SteamID, out Game game))
            {
                return true;
            }

            game.OnChangeFiremode(gPlayer);
            return false;
        }
    }
}
