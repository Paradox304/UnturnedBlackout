using HarmonyLib;
using SDG.Unturned;

namespace UnturnedBlackout.Patches
{
    [HarmonyPatch(typeof(PlayerClothing))]
    public static class ClothingPatches
    {
        [HarmonyPatch("ReceiveSwapHatRequest")]
        [HarmonyPrefix]
        public static bool WearHatPrefix(byte page, byte x, byte y)
        {
            return false;
        }

        [HarmonyPatch("ReceiveSwapBackpackRequest")]
        [HarmonyPrefix]
        public static bool WearBackpackPrefix(byte page, byte x, byte y)
        {
            if (page == 255)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch("ReceiveSwapVestRequest")]
        [HarmonyPrefix]
        public static bool WearVestPrefix(byte page, byte x, byte y)
        {
            return false;
        }

        [HarmonyPatch("ReceiveSwapMaskRequest")]
        [HarmonyPrefix]
        public static bool WearMaskPrefix(byte page, byte x, byte y)
        {
            return false;
        }

        [HarmonyPatch("ReceiveSwapShirtRequest")]
        [HarmonyPrefix]
        public static bool WearShirtPrefix(byte page, byte x, byte y)
        {
            return false;
        }

        [HarmonyPatch("ReceiveSwapPantsRequest")]
        [HarmonyPrefix]
        public static bool WearPantsPrefix(byte page, byte x, byte y)
        {
            return false;
        }

        [HarmonyPatch("ReceiveSwapGlassesRequest")]
        [HarmonyPrefix]
        public static bool WearGlassesPrefix(byte page, byte x, byte y)
        {
            return false;
        }
    }
}
