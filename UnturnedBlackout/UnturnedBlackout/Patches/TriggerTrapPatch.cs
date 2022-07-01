﻿using HarmonyLib;
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
        public static void Prefix(Collider other, InteractableTrap __instance)
        {
            if (!other.transform.CompareTag("Player"))
            {
                return;
            }

            if (!Provider.isPvP || other.transform.CompareTag("Vehicle"))
            {
                return;
            }

            var player = DamageTool.getPlayer(other.transform);
            var drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform.parent);
            if (player == null)
            {
                return;
            }

            if (drop == null)
            {
                return;
            }

            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(player);
            if (gPlayer == null)
            {
                return;
            }
            if (!Plugin.Instance.GameManager.TryGetCurrentGame(gPlayer.SteamID, out Game game))
            {
                return;
            }
            var data = drop.GetServersideData();
            if (data == null)
            {
                return;
            }

            if (gPlayer.SteamID.m_SteamID == data.owner || (data.group != 0UL && data.group == gPlayer.Player.Player.quests.groupID.m_SteamID))
            {
                return;
            }

            game.OnTrapTriggered(gPlayer, drop);
            return;
        }
    }
}
