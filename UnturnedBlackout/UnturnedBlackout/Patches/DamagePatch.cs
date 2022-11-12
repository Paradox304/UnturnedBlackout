using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;
using UnturnedBlackout.Extensions;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout.Patches;

[HarmonyPatch(typeof(CollisionUtil), nameof(CollisionUtil.ClosestPoint), typeof(GameObject), typeof(Vector3), typeof(bool))]
public static class ClosestPointPatch
{
    [HarmonyPrefix]
    public static bool Prefix(GameObject gameObject, Vector3 position, bool includeInactive, ref Vector3 __result)
    {
        __result = gameObject.transform.position;
        return false;
    }
}