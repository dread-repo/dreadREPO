using System;
using System.Reflection;
using Dread.Config;
using Dread.Systems.Core;
using HarmonyLib;
using UnityEngine.AI;

namespace Dread.Systems
{
    // Aggression patch: multiply NavMeshAgent speed/acceleration at Awake (host-only).
    internal static class EnemyNavMeshAgentAwakePatch
    {
        private static MethodInfo? _original;

        // EnemyNavMeshAgent.Agent flipped from public to internal in newer REPO
        // builds, so a direct __instance.Agent no longer compiles across versions.
        // Resolve the field by reflection (works for public or internal) and cache it.
        private static FieldInfo? _agentField;

        internal static void Apply(Harmony harmony)
        {
            if (_original != null || DreadConfig.CompatibilityMode.Value)
                return;

            var type = typeof(EnemyNavMeshAgent);
            _agentField = AccessTools.Field(type, "Agent");
            _original = AccessTools.Method(type, "Awake");
            if (_original == null)
            {
                LoggingService.LogWarning("[Dread] EnemyNavMeshAgent.Awake not found; aggression patch skipped");
                return;
            }

            if (HarmonyPatchCompat.ShouldSkipDueToForeignPatches(_original, "EnemyNavMeshAgent.Awake"))
                return;

            harmony.Patch(_original, postfix: new HarmonyMethod(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)));
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(_original, AccessTools.Method(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)));
            _original = null;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(EnemyNavMeshAgent __instance)
        {
            if (!DreadConfig.MonsterAggressionEnabled.Value || DreadConfig.CompatibilityMode.Value)
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;

            try
            {
                var agent = _agentField?.GetValue(__instance) as NavMeshAgent;
                if (agent == null) return;

                agent.speed *= 1.2f;
                agent.acceleration *= 1.2f;
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[Dread] EnemyNavMeshAgent patch skipped: {ex.Message}");
            }
        }
    }
}
