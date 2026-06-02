using Dread.Systems.UI;
using UnityEngine;

namespace Dread.Systems
{
    public partial class DebugOverlaySystem
    {
        private const byte RowNormal = 0;
        private const byte RowHeader = 1;
        private const byte RowSep = 2;
        private const byte RowSection = 3;

        // Palette aliases onto the shared Slate HUD "S2" theme (UI-1). Kept as local
        // names so the row builders in DebugOverlayPanel read naturally; the values
        // live in one place now (DreadTheme).
        private static readonly Color ColAccent = DreadTheme.Accent;
        private static readonly Color ColRail = DreadTheme.Rail;
        private static readonly Color ColSection = DreadTheme.Section;
        private static readonly Color ColDim = DreadTheme.Dim;
        private static readonly Color ColValue = DreadTheme.Value;
        private static readonly Color ColGood = DreadTheme.Good;
        private static readonly Color ColWarn = DreadTheme.Warn;
        private static readonly Color ColBad = DreadTheme.Bad;
        private static readonly Color ColButton = DreadTheme.Button;
        private static readonly Color ColButtonHover = DreadTheme.ButtonHover;

        private Texture2D? _bgTex;
        private Texture2D? _sepTex;
        private Texture2D? _railTex;
        private Texture2D? _buttonTex;
        private Texture2D? _buttonHoverTex;
        private GUIStyle? _boxStyle;
        private GUIStyle? _railStyle;
        private GUIStyle? _headerStyle;
        private GUIStyle? _hintStyle;
        private GUIStyle? _labelStyle;
        private GUIStyle? _valueStyle;
        private GUIStyle? _sectionStyle;
        private GUIStyle? _sectionBtnStyle;
        private GUIStyle? _caretStyle;
        private GUIStyle? _sepStyle;
        private GUIStyle? _buttonStyle;
        private GUIStyle? _midStyle;

        private void EnsureStyles()
        {
            if (_boxStyle != null)
                return;

            // Panel fill uses the configured opacity (DBG-2) over the theme's RGB.
            var bg = DreadTheme.PanelBg;
            _bgTex = DreadGui.SolidTexture(new Color(bg.r, bg.g, bg.b, _panelOpacity));
            _sepTex = DreadGui.SolidTexture(DreadTheme.Separator);
            _railTex = DreadGui.SolidTexture(ColRail);

            // FlatBox zeroes the inherited 9-slice border so thin fills (the 9x1
            // section tick, the hairline separator) blit flat instead of bloating
            // into a square. See DreadGui.FlatBox for the full rationale.
            _boxStyle = DreadGui.FlatBox(_bgTex);
            _railStyle = DreadGui.FlatBox(_railTex);
            _sepStyle = DreadGui.FlatBox(_sepTex);

            // MiddleLeft vertically centers each label in its row box so glyphs
            // line up with the steel ticks and separators drawn at mid-line.
            _headerStyle = DreadGui.Label(15, ColAccent, TextAnchor.MiddleLeft);
            _hintStyle = DreadGui.Label(11, ColDim, TextAnchor.MiddleRight);
            _labelStyle = DreadGui.Label(13, ColDim, TextAnchor.MiddleLeft);
            _valueStyle = DreadGui.Label(13, ColValue, TextAnchor.MiddleLeft);
            _sectionStyle = DreadGui.Label(11, ColSection, TextAnchor.MiddleLeft);

            // Transparent button over the section label so the row folds on click.
            // No background = invisible chrome; it reads as the section label itself.
            _sectionBtnStyle = DreadGui.Label(11, ColSection, TextAnchor.MiddleLeft);
            _sectionBtnStyle.hover.textColor = ColAccent;
            _sectionBtnStyle.active.textColor = ColAccent;

            // Fold caret, right-aligned in the section row.
            _caretStyle = DreadGui.Label(11, ColDim, TextAnchor.MiddleRight);

            // Centered numeric readout (zoom %, slider %) so the value sits under
            // its control instead of hugging the left edge.
            _midStyle = DreadGui.Label(11, ColValue, TextAnchor.MiddleCenter);

            _buttonTex = DreadGui.SolidTexture(ColButton);
            _buttonHoverTex = DreadGui.SolidTexture(ColButtonHover);
            _buttonStyle = DreadGui.Button(11, _buttonTex, _buttonHoverTex, ColValue);
            _buttonStyle.hover.textColor = ColAccent;
            _buttonStyle.active.textColor = ColAccent;
        }
    }
}
