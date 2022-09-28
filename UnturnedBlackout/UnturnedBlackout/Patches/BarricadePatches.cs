using HarmonyLib;
using SDG.Unturned;

namespace UnturnedBlackout.Patches;
public class BarricadePatches
{
    [HarmonyPatch(typeof(BarricadeManager), nameof(BarricadeManager.ReceiveDestroyBarricade))]
    [HarmonyPrefix]
    private static void BarricadeDestroyed(NetId netId)
    {
        Logging.Debug("Barricade destroy patch detected");
        var barricade = NetIdRegistry.Get<BarricadeDrop>(netId);
        if (barricade == null)
        {
            return;
        }

        Logging.Debug("Drop found, sending to all games");
        foreach (var game in Plugin.Instance.Game.Games)
        {
            game.OnBarricadeDestroyed(barricade);
        }
    }
}
