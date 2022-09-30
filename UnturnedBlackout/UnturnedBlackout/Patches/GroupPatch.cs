using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
// ReSharper disable InconsistentNaming

namespace UnturnedBlackout.Patches;

[HarmonyPatch(typeof(GroupManager), "requestGroupExit"), UsedImplicitly]
public static class OnExitGroup_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Player player, GroupManager __instance) => false;
}