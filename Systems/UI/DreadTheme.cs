using UnityEngine;

namespace Dread.Systems.UI
{
    /// <summary>
    /// Canonical "Slate HUD S2" monochrome palette, pulled from the R.E.P.O. icon:
    /// void black, brushed-steel grays, soft white. No hue, no gradients.
    ///
    /// Single source of truth for the debug overlay, the component-kit widgets, and
    /// any future Dread panel. Severity tints (warn/bad) carry just enough hue to
    /// read at a glance without leaving the monochrome family. Consumers that need a
    /// deliberately different look (for example the error-reporting prompt's warm
    /// accent) keep their own colors and reuse only <see cref="DreadGui"/> helpers.
    /// </summary>
    internal static class DreadTheme
    {
        public static readonly Color Accent = new(0.80f, 0.81f, 0.83f);   // header text (steel highlight)
        public static readonly Color Rail = new(0.74f, 0.75f, 0.77f);     // accent rail + section ticks
        public static readonly Color Section = new(0.60f, 0.61f, 0.64f);  // section labels (mid steel)
        public static readonly Color Dim = new(0.44f, 0.45f, 0.49f);      // keys and muted values (steel low)
        public static readonly Color Value = new(0.91f, 0.92f, 0.93f);    // primary values (soft white)
        public static readonly Color Good = new(0.79f, 0.81f, 0.79f);     // status ok (light neutral)
        public static readonly Color Warn = new(0.85f, 0.79f, 0.65f);     // status warn (warm gray)
        public static readonly Color Bad = new(0.84f, 0.70f, 0.68f);      // status bad (rosy gray)

        public static readonly Color PanelBg = new(0.055f, 0.058f, 0.070f, 0.90f);     // panel fill
        public static readonly Color Separator = new(0.85f, 0.86f, 0.88f, 0.18f);      // hairline rule
        public static readonly Color Button = new(0.11f, 0.12f, 0.14f, 0.92f);         // button rest
        public static readonly Color ButtonHover = new(0.17f, 0.18f, 0.21f, 0.96f);    // button hover
        public static readonly Color Track = new(0.20f, 0.21f, 0.24f, 0.85f);          // slider/bar track
    }
}
