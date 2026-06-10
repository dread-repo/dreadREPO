using HarmonyLib;

namespace Dread.Systems.Core
{
    /// <summary>
    /// REPOConfig-specific Harmony compat (slider labels only). REPOConfig has no API for bool toggle descriptions.
    /// </summary>
    internal static class RepoConfigCompat
    {
        internal static void TryApply(Harmony harmony)
        {
            RepoConfigSliderLabelCompat.TryApply(harmony);
        }

        /// <summary>Reset apply state after the owning Harmony instance unpatched itself (plugin unload).</summary>
        internal static void Shutdown()
        {
            RepoConfigSliderLabelCompat.Reset();
        }
    }
}
