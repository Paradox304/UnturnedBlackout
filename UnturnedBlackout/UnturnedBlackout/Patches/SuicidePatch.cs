using HarmonyLib;
using SDG.Unturned;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Patches;

[HarmonyPatch(typeof(PlayerLife), nameof(PlayerLife.ReceiveSuicideRequest))]
public static class SuicidePatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerLife __instance)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(__instance.player);
        if (gPlayer == null)
            return true;

        return gPlayer.CurrentGame != null && gPlayer.CurrentGame.GamePhase != EGamePhase.ENDING && gPlayer.CurrentGame.GamePhase != EGamePhase.STARTING;
    }
}