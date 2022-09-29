using HarmonyLib;
using SDG.Unturned;

namespace UnturnedBlackout.Patches;

[HarmonyPatch(typeof(BarricadeManager), nameof(BarricadeManager.ReceiveDestroyBarricade))]
public static class BarricadePatches
{
    [HarmonyPrefix]
    public static void BarricadeDestroyed(NetId netId)
    {
        Logging.Debug("Barricade destroy patch detected");
        var drop = NetIdRegistry.Get<BarricadeDrop>(netId);
        if (drop == null)
        {
            return;
        }
        
        Logging.Debug("Drop found, sending to all games");
        foreach (var game in Plugin.Instance.Game.Games)
        {
            game.OnBarricadeDestroyed(drop);
        }
    }
}
