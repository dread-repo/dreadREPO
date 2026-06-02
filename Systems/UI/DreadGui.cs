using UnityEngine;

namespace Dread.Systems.UI
{
    /// <summary>
    /// Shared IMGUI primitives for Dread's in-game panels (UI-1). Centralizes the
    /// boilerplate every overlay/prompt/toast used to copy by hand: an empty
    /// <see cref="GUIContent"/>, Proton-safe solid textures, and style builders that
    /// zero the inherited 9-slice border so thin fills stay crisp.
    ///
    /// All draw helpers must run inside an OnGUI pass.
    /// </summary>
    internal static class DreadGui
    {
        /// <summary>
        /// Reusable empty label content. Avoids <c>GUIContent.none</c>, which the
        /// game's stripped UnityEngine resolves against a stub <c>get_none</c> that
        /// does not exist, throwing MissingMethodException inside OnGUI.
        /// </summary>
        public static readonly GUIContent EmptyContent = new();

        /// <summary>
        /// 1x1 solid texture via <see cref="OverlayTextureUtil"/> (which probes
        /// formats so it survives Proton/Linux GPU stacks). Falls back to a raw
        /// Texture2D if every probed format is rejected.
        /// </summary>
        public static Texture2D SolidTexture(Color color)
        {
            var tex = OverlayTextureUtil.CreateSolid(color);
            if (tex != null)
                return tex;

            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Box style backed by a flat fill. The inherited <c>GUI.skin.box</c> border
        /// is zeroed: a ~6px 9-slice border bloats any rect small in both dimensions
        /// (a thin tick or a shrinking life bar) into a chunky square. A zero border
        /// blits the texture flat at any size.
        /// </summary>
        public static GUIStyle FlatBox(Texture2D background)
        {
            var style = new GUIStyle(GUI.skin.box) { border = new RectOffset(0, 0, 0, 0) };
            style.normal.background = background;
            return style;
        }

        /// <summary>Convenience: a flat box filled with a solid color.</summary>
        public static GUIStyle FlatBox(Color color) => FlatBox(SolidTexture(color));

        /// <summary>Label style with the given size, color, anchor, and wrap.</summary>
        public static GUIStyle Label(
            int fontSize,
            Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft,
            bool wordWrap = false)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = anchor,
                wordWrap = wordWrap,
            };
            style.normal.textColor = color;
            return style;
        }

        /// <summary>
        /// Button style with rest/hover backgrounds and a single text color across
        /// all states. Hover and active share the same highlight texture.
        /// </summary>
        public static GUIStyle Button(int fontSize, Texture2D rest, Texture2D hover, Color text)
        {
            var style = new GUIStyle(GUI.skin.button) { fontSize = fontSize, wordWrap = false };
            style.normal.background = rest;
            style.hover.background = hover;
            style.active.background = hover;
            style.normal.textColor = text;
            style.hover.textColor = text;
            style.active.textColor = text;
            return style;
        }
    }
}
