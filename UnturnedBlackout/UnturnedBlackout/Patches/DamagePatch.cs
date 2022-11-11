using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Rocket.Core.Logging;
using SDG.Unturned;

namespace UnturnedBlackout.Patches;

[HarmonyPatch(typeof(DamageTool), nameof(DamageTool.damagePlayer))]
public static class DamagePatch
{
    [HarmonyPrefix, UsedImplicitly]
    public static void Prefix(DamagePlayerParameters parameters, ref EPlayerKill kill)
    {
        Logger.Log($"PRE DAMAGE PLAYER");
        Logger.Log($"Kill: {kill}");
        Logger.Log($"Victim: {parameters.player.channel.owner.playerID.characterName}, Victim's Pos: {parameters.player.transform.position}, killer: {PlayerTool.getPlayer(parameters.killer)?.channel.owner.playerID.characterName ?? "None"}, damage: {parameters.damage}, times: {parameters.times}, armor: {DamageTool.getPlayerArmor(parameters.limb, parameters.player)}, Apply global armor: {parameters.applyGlobalArmorMultiplier}, Global Armor: {Provider.modeConfigData.Players.Armor_Multiplier}");
    }

    [HarmonyPostfix, UsedImplicitly]
    public static void Postfix(DamagePlayerParameters parameters, ref EPlayerKill kill)
    {
        Logger.Log($"POST DAMAGE PLAYER");
        Logger.Log($"Kill: {kill}");
        Logger.Log($"Victim: {parameters.player.channel.owner.playerID.characterName}, Victim's Pos: {parameters.player.transform.position}, killer: {PlayerTool.getPlayer(parameters.killer)?.channel.owner.playerID.characterName ?? "None"}, damage: {parameters.damage}, times: {parameters.times}, armor: {DamageTool.getPlayerArmor(parameters.limb, parameters.player)}, Apply global armor: {parameters.applyGlobalArmorMultiplier}, Global Armor: {Provider.modeConfigData.Players.Armor_Multiplier}");
    }
}

[HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode))]
public static class GrenadeExplodePatch
{
    [HarmonyPrefix, UsedImplicitly]
    public static void Prefix(Grenade __instance)
    {
        Logger.Log($"PRE GRENADE EXPLODE");
        Logger.Log($"Range: {__instance.range}, Player Damage: {__instance.playerDamage}, Point: {__instance.transform.position}");
    }
}
