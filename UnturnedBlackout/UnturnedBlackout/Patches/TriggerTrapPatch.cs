using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using UnturnedBlackout.Extensions;

// ReSharper disable InconsistentNaming

namespace UnturnedBlackout.Patches;

[HarmonyPatch(typeof(InteractableTrap), "OnTriggerEnter")]
public static class TriggerTrapPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Collider other, InteractableTrap __instance)
    {
        if (!other.transform.CompareTag("Player"))
            return true;

        if (!Provider.isPvP || other.transform.CompareTag("Vehicle"))
            return true;

        var player = DamageTool.getPlayer(other.transform);
        var drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform.parent);
        if (player == null)
            return true;

        if (drop == null)
            return true;

        var gPlayer = Plugin.Instance.Game.GetGamePlayer(player);

        var game = gPlayer?.CurrentGame;
        if (game == null)
            return true;

        var data = drop.GetServersideData();
        if (data == null)
            return true;

        Logging.Debug($"Player: {gPlayer.SteamID.m_SteamID}, owner: {data.owner}, player group: {gPlayer.Player.Player.quests.groupID.m_SteamID}, trap group: {data.group}");
        if (data.group != 0UL && data.group == gPlayer.Player.Player.quests.groupID.m_SteamID)
        {
            Logging.Debug("Player stepping on his own trap or teammmate's");
            return false;
        }

        game.OnTrapTriggered(gPlayer, drop);
        return true;
    }
}