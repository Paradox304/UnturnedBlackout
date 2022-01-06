using HarmonyLib;
using SDG.Unturned;

namespace UnturnedBlackout.Patches
{
    [HarmonyPatch(typeof(GroupManager), "requestGroupExit")]
    public static class OnExitGroup_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player player, GroupManager __instance)
        {
            return false;
        }
    }
}
