using HarmonyLib;
using Rocket.Core.Utils;
using SDG.Unturned;
using UnityEngine;
using UnturnedBlackout.GameTypes;

namespace UnturnedBlackout.Patches
{
    [HarmonyPatch(typeof(InteractableTrap), "OnTriggerEnter")]
    public static class TriggerTrapPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Collider other, InteractableTrap __instance)
        {
            if (!other.transform.CompareTag("Player"))
            {
                return true;
            }

            if (!Provider.isPvP || other.transform.CompareTag("Vehicle"))
            {
                return false;
            }

            var player = DamageTool.getPlayer(other.transform);
            var drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform.parent);
            if (player == null)
            {
                return true;
            }

            if (drop == null)
            {
                return true;
            }

            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(player);
            if (gPlayer == null)
            {
                return true;
            }
            if (!Plugin.Instance.GameManager.TryGetCurrentGame(gPlayer.SteamID, out Game game))
            {
                return true;
            }
            var data = drop.GetServersideData();
            if (data == null)
            {
                return true;
            }

            if (gPlayer.SteamID.m_SteamID == data.owner || (data.group != 0UL && data.group == gPlayer.Player.Player.quests.groupID.m_SteamID))
            {
                return false;
            }

            TaskDispatcher.QueueOnMainThread(() => game.OnTrapTriggered(gPlayer, drop));
            return true;
        }
    }
}
