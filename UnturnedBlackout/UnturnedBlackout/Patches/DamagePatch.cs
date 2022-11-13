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

/*[HarmonyPatch(typeof(CollisionUtil), nameof(CollisionUtil.ClosestPoint), typeof(GameObject), typeof(Vector3), typeof(bool))]
public static class ClosestPointPatch
{
    [HarmonyPrefix]
    public static bool Prefix(GameObject gameObject, Vector3 position, bool includeInactive, ref Vector3 __result)
    {
        __result = gameObject.transform.position;
        return false;
    }
}*/

[HarmonyPatch]
public static class ClosestPointPatch2
{
    public static MethodBase TargetMethod()
    {
        var method = AccessTools.FirstMethod(typeof(CollisionUtil), method => method.ReturnType == typeof(bool) && method.Name.Contains("ClosestPoint"));
        return method;
    }

    [HarmonyPrefix]
    public static void Prefix(IEnumerable<Collider> colliders, Vector3 position, ref Vector3 result)
    {
        Logger.Log($"PRE CLOSEST POINT 2");
        Logger.Log($"Colliders: {colliders.Count()}, Pos: {position}");
       
        var flag = false;
        var res = default(Vector3);
        var num = -1f;
        foreach (var collider in colliders)
        {
            if (!(collider == null) && collider.enabled && !collider.isTrigger)
            {
                Logger.Log($"Collider is not null, collder is enabled, collider is not trigger, pos: {collider.transform.position}");
                var bounds = collider.bounds;
                Logger.Log($"Collider path: {collider.transform.GetPath()}, Center: {bounds.center}, Min: {bounds.min}, Max: {bounds.max}, Size: {bounds.size}, Extents: {bounds.extents}");
                var meshCollider = collider as MeshCollider;
                if (meshCollider != null)
                {
                    Logger.Log("Collider is a mesh collider, checking if it's convex");
                    if (!meshCollider.convex)
                    {
                        Logger.Log("Mesh is not convex, continue");
                        continue;
                    }
                }
                else if (!(collider is BoxCollider) && !(collider is SphereCollider) && !(collider is CapsuleCollider))
                {
                    Logger.Log("Collider is not box, sphere or capsule collider, continue");
                    continue;
                }
                var vector = collider.ClosestPoint(position);
                Logger.Log($"Collider closest point to position: {vector}");
                var sqrMagnitude = (vector - position).sqrMagnitude;
                Logger.Log($"Sqr magnitude between vector and position: {sqrMagnitude}");
               
                Logger.Log($"Flag is {flag}, sqr magnitude is {sqrMagnitude}, num is {num}");
                if (flag)
                {
                    if (sqrMagnitude < num)
                    {
                        Logger.Log("Num is greater than sqr magntitude, set vector to res and num to sqrmagnitude");
                        res = vector;
                        num = sqrMagnitude;
                    }
                }
                else
                {
                    Logger.Log("Flag is false, set flag to true, res to vector and num to sqr magnitude");
                    flag = true;
                    res = vector;
                    num = sqrMagnitude;
                }
            }
            else
                Logger.Log("Collider is either null, not enabled or triggered");
        }
       
        Logger.Log($"Result: {res}, bool: {flag}");
    }
}