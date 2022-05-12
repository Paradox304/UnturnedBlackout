using HarmonyLib;
using SDG.Unturned;

namespace UnturnedBlackout.Patches
{
    // For disabling reputation UI pop up
    [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askRep))]
    public static class ReputationPatch
    {
        public static bool Prefix(int rep)
        {
            return false;
        }
    }
}