using HarmonyLib;
using SDG.Unturned;
// ReSharper disable InconsistentNaming

namespace UnturnedBlackout.Patches;

[HarmonyPatch(typeof(PlayerInventory))]
public static class InventoryPatches
{
    [HarmonyPatch("ReceiveDragItem"), HarmonyPrefix]
    public static bool ReceiveDragItem(byte page_0, byte x_0, byte y_0, byte page_1, byte x_1, byte y_1, byte rot_1) => false;

    [HarmonyPatch("ReceiveSwapItem"), HarmonyPrefix]
    public static bool ReceiveSwapItem(byte page_0, byte x_0, byte y_0, byte rot_0, byte page_1, byte x_1, byte y_1, byte rot_1) => false;

    [HarmonyPatch("ReceiveDropItem"), HarmonyPrefix]
    public static bool ReceiveDropItem(byte page, byte x, byte y) => false;
}