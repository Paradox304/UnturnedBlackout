using HarmonyLib;
using SDG.Unturned;

namespace UnturnedBlackout.Patches;
public class BarricadePatches
{
    [HarmonyPatch(typeof(BarricadeManager), nameof(BarricadeManager.destroyBarricade))]
    [HarmonyPrefix]
    private static void BarricadeDestroyed(BarricadeDrop barricade, byte x, byte y, ushort plant)
    {
        Logging.Debug("Barricade destroy patch detected. Drop found, sending to all games");
        foreach (var game in Plugin.Instance.Game.Games)
        {
            game.OnBarricadeDestroyed(barricade);
        }
    }
}
