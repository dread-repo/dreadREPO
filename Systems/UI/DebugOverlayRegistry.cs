using System;
using System.Collections.Generic;

namespace Dread.Systems.UI
{
    /// <summary>
    /// Semantic status for an overlay row. Maps to the Slate HUD status colors so
    /// callers never touch <see cref="UnityEngine.Color"/> or the theme directly.
    /// </summary>
    public enum OverlayStatus : byte
    {
        Normal,
        Dim,
        Good,
        Warn,
        Bad,
    }

    /// <summary>Receives rows emitted by a section during a build pass.</summary>
    public interface IOverlayRowSink
    {
        /// <summary>Add a label/value row. Status picks the value color.</summary>
        void Row(string label, string value, OverlayStatus status = OverlayStatus.Normal);
    }

    /// <summary>
    /// A named, collapsible overlay section contributed by a feature. The overlay
    /// rebuilds rows every frame, so <see cref="Build"/> is called each frame while
    /// the panel is visible: keep it allocation-light and side-effect free.
    /// </summary>
    public interface IOverlaySection
    {
        string Title { get; }
        void Build(IOverlayRowSink sink);
    }

    /// <summary>
    /// Extensibility point for the debug overlay (DBG-5). Features register a
    /// section here instead of editing <c>DebugOverlaySystem</c>; the overlay
    /// appends every registered section after its built-in ones, each foldable and
    /// persisted like the rest. Registration is safe even in production builds
    /// where the overlay is stripped: the list is simply never drained.
    ///
    /// Thread-affinity: register/unregister from the main thread (these run during
    /// system init and teardown, not from background threads).
    /// </summary>
    public static class DebugOverlayRegistry
    {
        private static readonly List<IOverlaySection> SectionList = new();

        /// <summary>Registered sections in registration order.</summary>
        public static IReadOnlyList<IOverlaySection> Sections => SectionList;

        /// <summary>Register a section. Ignores null and duplicate instances.</summary>
        public static void Register(IOverlaySection section)
        {
            if (section == null || SectionList.Contains(section))
                return;

            SectionList.Add(section);
        }

        /// <summary>Register a section from a title and a build delegate.</summary>
        public static IOverlaySection Register(string title, Action<IOverlayRowSink> build)
        {
            var section = new DelegateSection(title, build);
            SectionList.Add(section);
            return section;
        }

        /// <summary>Remove a previously registered section.</summary>
        public static void Unregister(IOverlaySection section)
        {
            if (section != null)
                SectionList.Remove(section);
        }

        private sealed class DelegateSection : IOverlaySection
        {
            private readonly Action<IOverlayRowSink> _build;

            public DelegateSection(string title, Action<IOverlayRowSink> build)
            {
                Title = title ?? string.Empty;
                _build = build ?? (_ => { });
            }

            public string Title { get; }

            public void Build(IOverlayRowSink sink) => _build(sink);
        }
    }
}
