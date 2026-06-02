using UnityEngine;

namespace Dread.Systems.UI
{
    /// <summary>
    /// Resolves a legible IMGUI font for Dread panels (DBG-3). The default IMGUI
    /// font is Arial, which is frequently absent on Proton/Linux and renders as
    /// blank boxes or falls back to an ugly bitmap face. A dynamic font built from
    /// an OS-font fallback chain renders consistently on Windows and Proton alike.
    ///
    /// The resolved font is cached and applied per <see cref="GUIStyle"/>; glyph
    /// size still comes from each style's <c>fontSize</c>, so a single dynamic font
    /// serves every size in the kit.
    /// </summary>
    internal static class DreadFont
    {
        // Ordered preference: monospace first (the HUD is tabular), then common
        // sans faces that ship on Windows, most Linux distros, and Proton prefixes.
        private static readonly string[] Candidates =
        {
            "Consolas",
            "DejaVu Sans Mono",
            "Liberation Mono",
            "Noto Sans Mono",
            "Liberation Sans",
            "DejaVu Sans",
            "Arial",
        };

        private static Font? _font;
        private static bool _resolved;

        /// <summary>The cached dynamic font, or null if none could be created.</summary>
        public static Font? Resolve()
        {
            if (_resolved)
                return _font;

            _resolved = true;

            // Guarded: CreateDynamicFontFromOSFont can throw on stripped or
            // headless runtimes. Fall back to the skin font, then to null (which
            // leaves the style on Unity's built-in default).
            try
            {
                _font = Font.CreateDynamicFontFromOSFont(Candidates, 14);
            }
            catch
            {
                _font = null;
            }

            if (_font == null)
            {
                try
                {
                    _font = GUI.skin.font;
                }
                catch
                {
                    _font = null;
                }
            }

            return _font;
        }

        /// <summary>Apply the resolved font to a style, if one is available.</summary>
        public static void Apply(GUIStyle style)
        {
            var font = Resolve();
            if (font != null)
                style.font = font;
        }
    }
}
