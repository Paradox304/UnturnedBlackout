using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Patches
{
    [HarmonyPatch(typeof(PlayerInventory))]
    public static class InventoryPatches
    {
        [HarmonyPatch("ReceiveDragItem")]
        [HarmonyPrefix]
        public static bool ReceiveDragItem(byte page_0, byte x_0, byte y_0, byte page_1, byte x_1, byte y_1, byte rot_1)
        {
            return false;
        }

        [HarmonyPatch("ReceiveSwapItem")]
        [HarmonyPrefix]
        public static bool ReceiveSwapItem(byte page_0, byte x_0, byte y_0, byte rot_0, byte page_1, byte x_1, byte y_1, byte rot_1)
        {
            return false;
        }

        [HarmonyPatch("ReceiveDropItem")]
        [HarmonyPrefix]
        public static bool ReceiveDropItem(byte page, byte x, byte y)
        {
            return false;
        }
    }
}
