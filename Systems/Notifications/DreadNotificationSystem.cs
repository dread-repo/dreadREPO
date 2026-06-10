using System.Collections.Generic;
using Dread.Systems.UI;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Severity of a transient notification toast. Conveyed by the rail color
    /// (no icons) per the Slate HUD "S2" design.
    /// </summary>
    public enum DreadToastKind : byte
    {
        Info,
        Warn,
        Bad,
    }

    /// <summary>
    /// Transient corner notifications ("toasts") in the Slate HUD S2 monochrome
    /// style: dark slab, solid steel left rail (tinted by severity), soft white
    /// title, dim message, and a shrinking life bar. Toasts slide in from the
    /// right, hold, then slide out and self-dismiss.
    ///
    /// Any system raises one with <see cref="Info"/>, <see cref="Warn"/>, or
    /// <see cref="Bad"/> from any thread; the queue is drained on the main thread.
    /// </summary>
    public class DreadNotificationSystem : MonoBehaviour
    {
        private const int GuiDepth = 9000;
        private const float ToastWidth = 312f;
        private const float RailWidth = 2f;
        private const float PadX = 14f;
        private const float PadY = 12f;
        private const float MarginX = 26f;
        private const float MarginBottom = 26f;
        private const float Gap = 12f;
        private const float TitleLineH = 20f;
        private const float LifeSeconds = 5f;
        private const float SlideSeconds = 0.28f;

        private static readonly object Gate = new();
        private static readonly List<Pending> PendingQueue = new(8);

        private readonly List<Toast> _toasts = new(8);

        // Slate HUD "S2" monochrome palette. No hue, no gradients.
        private static readonly Color ColPanel = new(0.063f, 0.067f, 0.078f, 0.96f);
        private static readonly Color ColTitle = new(0.91f, 0.92f, 0.93f);
        private static readonly Color ColMessage = new(0.52f, 0.53f, 0.57f);
        // Severity reads at a glance but stays desaturated enough for the Slate
        // HUD: cool steel info, amber warn, muted red bad (#C2C4C9/#D8B45A/#C9605A).
        private static readonly Color ColRailInfo = new(0.761f, 0.769f, 0.788f); // cool steel
        private static readonly Color ColRailWarn = new(0.847f, 0.706f, 0.353f); // amber
        private static readonly Color ColRailBad = new(0.788f, 0.376f, 0.353f);  // muted red

        private static readonly GUIContent ScratchContent = new();

        private Texture2D? _panelTex;
        private Texture2D? _railInfoTex;
        private Texture2D? _railWarnTex;
        private Texture2D? _railBadTex;
        private GUIStyle? _panelStyle;
        private GUIStyle? _railStyle;
        private GUIStyle? _titleStyle;
        private GUIStyle? _messageStyle;

        /// <summary>Raise an informational toast.</summary>
        public static void Info(string title, string message) => Show(DreadToastKind.Info, title, message);

        /// <summary>Raise a warning toast.</summary>
        public static void Warn(string title, string message) => Show(DreadToastKind.Warn, title, message);

        /// <summary>Raise an error toast.</summary>
        public static void Bad(string title, string message) => Show(DreadToastKind.Bad, title, message);

        /// <summary>Queue a toast. Safe to call from any thread.</summary>
        public static void Show(DreadToastKind kind, string title, string message)
        {
            lock (Gate)
            {
                PendingQueue.Add(new Pending
                {
                    Kind = kind,
                    Title = title ?? string.Empty,
                    Message = message ?? string.Empty,
                });
            }
        }

        private void Update()
        {
            DrainPending();

            if (_toasts.Count == 0)
                return;

            float now = Time.realtimeSinceStartup;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (now - _toasts[i].Spawn > LifeSeconds + SlideSeconds)
                    _toasts.RemoveAt(i);
            }
        }

        private void DrainPending()
        {
            lock (Gate)
            {
                if (PendingQueue.Count == 0)
                    return;

                float now = Time.realtimeSinceStartup;
                foreach (var p in PendingQueue)
                    _toasts.Add(new Toast { Kind = p.Kind, Title = p.Title, Message = p.Message, Spawn = now });

                PendingQueue.Clear();
            }
        }

        private void OnGUI()
        {
            if (_toasts.Count == 0)
                return;

            EnsureStyles();
            if (_panelStyle == null || _railStyle == null || _titleStyle == null || _messageStyle == null)
                return;

            GUI.depth = GuiDepth;

            float now = Time.realtimeSinceStartup;
            float innerW = ToastWidth - RailWidth - PadX * 2f;
            float targetX = Screen.width - MarginX - ToastWidth;
            float y = Screen.height - MarginBottom;

            // Newest toast sits closest to the corner; stack older ones upward.
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var t = _toasts[i];
                float age = now - t.Spawn;

                ScratchContent.text = t.Message;
                float msgH = _messageStyle.CalcHeight(ScratchContent, innerW);
                float height = PadY * 2f + TitleLineH + 5f + msgH + 4f;

                float x = SlideX(age, targetX);
                y -= height;
                var box = new Rect(x, y, ToastWidth, height);

                GUI.Box(box, DreadGui.EmptyContent, _panelStyle);

                // Severity is carried entirely by the rail color (no icons).
                _railStyle.normal.background = RailTex(t.Kind);
                GUI.Box(new Rect(box.x, box.y, RailWidth, height), DreadGui.EmptyContent, _railStyle);

                float tx = box.x + RailWidth + PadX;
                float ty = box.y + PadY;
                GUI.Label(new Rect(tx, ty, innerW, TitleLineH), t.Title.ToUpperInvariant(), _titleStyle);
                GUI.Label(new Rect(tx, ty + TitleLineH + 5f, innerW, msgH), t.Message, _messageStyle);

                // Shrinking life bar across the bottom edge (steel). It starts
                // just right of the rail so the rail can run full height to the
                // bottom corner without the two bars overlapping.
                float life = Mathf.Clamp01(1f - age / LifeSeconds);
                _railStyle.normal.background = _railInfoTex;
                GUI.Box(
                    new Rect(box.x + RailWidth, box.y + height - 1f, (box.width - RailWidth) * life, 1f),
                    DreadGui.EmptyContent, _railStyle);

                y -= Gap;
            }
        }

        private static float SlideX(float age, float targetX)
        {
            float offX = Screen.width + 8f;
            if (age < SlideSeconds)
            {
                float t = EaseOut(age / SlideSeconds);
                return Mathf.Lerp(offX, targetX, t);
            }

            if (age > LifeSeconds)
            {
                float t = Mathf.Clamp01((age - LifeSeconds) / SlideSeconds);
                return Mathf.Lerp(targetX, offX, t);
            }

            return targetX;
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private Texture2D? RailTex(DreadToastKind kind) => kind switch
        {
            DreadToastKind.Warn => _railWarnTex,
            DreadToastKind.Bad => _railBadTex,
            _ => _railInfoTex,
        };

        private void EnsureStyles()
        {
            if (_panelStyle != null)
                return;

            _panelTex = DreadGui.SolidTexture(ColPanel);
            _railInfoTex = DreadGui.SolidTexture(ColRailInfo);
            _railWarnTex = DreadGui.SolidTexture(ColRailWarn);
            _railBadTex = DreadGui.SolidTexture(ColRailBad);

            // FlatBox zeroes the inherited 9-slice border so the 1px life bar stays
            // a flat line as it shrinks toward zero width instead of bloating into a
            // square. See DreadGui.FlatBox for the full rationale.
            _panelStyle = DreadGui.FlatBox(_panelTex);
            _railStyle = DreadGui.FlatBox(_railInfoTex);

            _titleStyle = DreadGui.Label(12, ColTitle, TextAnchor.UpperLeft);
            _messageStyle = DreadGui.Label(12, ColMessage, TextAnchor.UpperLeft, wordWrap: true);
        }

        private struct Pending
        {
            public DreadToastKind Kind;
            public string Title;
            public string Message;
        }

        private struct Toast
        {
            public DreadToastKind Kind;
            public string Title;
            public string Message;
            public float Spawn;
        }
    }
}
