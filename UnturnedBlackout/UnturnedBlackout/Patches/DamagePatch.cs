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
    public static bool Prefix(ref DamagePlayerParameters parameters, ref EPlayerKill kill)
    {
        if (parameters.player == null || parameters.player.life.isDead)
            return true;

        Logger.Log($"PRE DAMAGE PLAYER");
        Logger.Log($"Kill: {kill}");
        Logger.Log($"Victim: {parameters.player.channel.owner.playerID.characterName}, killer: {PlayerTool.getPlayer(parameters.killer)?.channel.owner.playerID.characterName ?? "None"}, damage: {parameters.damage}, times: {parameters.times}, armor: {DamageTool.getPlayerArmor(parameters.limb, parameters.player)}, Apply global armor: {parameters.applyGlobalArmorMultiplier}, Global Armor: {Provider.modeConfigData.Players.Armor_Multiplier}");
        var shouldAllow = true;
        foreach (var game in Plugin.Instance.Game.Games)
            game.OnPlayerDamage(ref parameters, ref shouldAllow);

        return shouldAllow;
    }

    [HarmonyPostfix, UsedImplicitly]
    public static void Postfix(DamagePlayerParameters parameters, ref EPlayerKill kill)
    {
        Logger.Log($"POST DAMAGE PLAYER");
        Logger.Log($"Kill: {kill}");
        Logger.Log($"Victim: {parameters.player.channel.owner.playerID.characterName}, killer: {PlayerTool.getPlayer(parameters.killer)?.channel.owner.playerID.characterName ?? "None"}, damage: {parameters.damage}, times: {parameters.times}, armor: {DamageTool.getPlayerArmor(parameters.limb, parameters.player)}, Apply global armor: {parameters.applyGlobalArmorMultiplier}, Global Armor: {Provider.modeConfigData.Players.Armor_Multiplier}");
    }
}