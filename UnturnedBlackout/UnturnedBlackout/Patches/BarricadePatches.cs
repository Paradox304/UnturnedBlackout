using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Patches;
public class BarricadePatches
{
    [HarmonyPatch(typeof(BarricadeManager), nameof(BarricadeManager.ReceiveDestroyBarricade))]
    [HarmonyPrefix]
    private static void BarricadeDestroyed(NetId netId)
    {
        var barricade = NetIdRegistry.Get<BarricadeDrop>(netId);
        if (barricade == null)
        {
            return;
        }

        foreach (var game in Plugin.Instance.Game.Games)
        {
            game.OnBarricadeDestroyed(barricade);
        }
    }
}
