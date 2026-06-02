# Code Review: Systems/UI (and related presentation layer)

**Reviewed:** 2026-06-01  
**Scope:** In-game presentation: IMGUI HUD/modals, canvas overlays, texture helpers, debug TCP tooling; registry and `DreadRuntimeState` consumers. Excludes `Systems/Core/` (see [01-core-review.md](01-core-review.md)).  
**Files inventoried:** `Systems/DebugOverlay/*`, `Systems/DebugServerSystem.cs`, `Systems/OverlayTextureUtil.cs`, `Systems/ErrorReporting/ErrorReportingPromptSystem.cs`, `Systems/PsychoticBreak/PsychoticBreakOverlay.cs`, `Systems/DreadRuntimeState.cs`, `Systems/DreadSystemRegistry.cs`, `Config/DreadConfig.cs` (overlay/prompt entries)

## Executive Summary

There is **no `Systems/UI/` folder** today. Player-visible UI is split across **`Systems/DebugOverlay/`** (F10 IMGUI HUD), **`Systems/ErrorReporting/ErrorReportingPromptSystem.cs`** (ERR-2 consent modal, IMGUI), **`Systems/PsychoticBreak/PsychoticBreakOverlay.cs`** (uGUI canvas vignette), and **`Systems/OverlayTextureUtil.cs`** (shared procedural textures). **`DebugServerSystem`** is remote debug tooling (ADR-0013), not on-screen UI, but it duplicates overlay state/patch counting and belongs in a **Debug** boundary next to the HUD.

**Notifications:** No in-game notification/toast system exists in this repository (no types, files, or registry rows). The only “prompt” UI is error-reporting consent. Roadmap **UI-1** already plans a shared IMGUI kit for the prompt and debug overlay.

Main risks: **duplicated IMGUI styling and Harmony patch enumeration**, **`ErrorReportingPromptSystem` living under ErrorReporting** (blurs domain vs presentation), **IMGUI textures not destroyed on teardown**, **shared `GUIStyle` mutation in the debug overlay row loop**, **PERF-2 partial gap** (FPS sampling still runs every frame when F10 hides the HUD), and **CI analyze globs skipping nested `Systems/**`** (same as Core review). `OverlayTextureUtil` is only used by psychotic break; the error prompt uses a simpler `MakeTexture` path that skips Proton-safe format probing.

## File Structure Assessment

### Current map (grep + file tree, 2026-06-01)

| Path | Role | UI bucket |
|------|------|-----------|
| `Systems/DebugOverlay/DebugOverlaySystem.cs` | Lifecycle, F10, FPS sampling, config gate | Debug IMGUI HUD |
| `Systems/DebugOverlay/DebugOverlayPanel.cs` | `OnGUI` layout, rows, `CountDreadPatches` | Debug IMGUI HUD |
| `Systems/DebugOverlay/DebugOverlayStyles.cs` | Theme textures + `GUIStyle` cache | Debug IMGUI HUD |
| `Systems/ErrorReporting/ErrorReportingPromptSystem.cs` | ERR-2 first-run modal (`OnGUI`, depth 10000) | Player IMGUI modal |
| `Systems/OverlayTextureUtil.cs` | Proton-safe `Texture2D` creation | Shared rendering util |
| `Systems/PsychoticBreak/PsychoticBreakOverlay.cs` | Runtime uGUI canvas + vignette | Gameplay screen effect (not debug) |
| `Systems/DebugServerSystem.cs` | TCP JSON commands, `get_runtime_state` | Debug tooling (no `OnGUI`) |
| `Systems/DreadRuntimeState.cs` | Snapshot for HUD + MCP | Data contract (ADR-0016) |
| `Systems/Core/RepoConfigSliderLabelCompat.cs` | REPOConfig/MenuLib slider layout | External config UI (reviewed in Core) |

### Proposed `Systems/UI/` layout

Align with ROADMAP **UI-1** / **DBG-5** without big-bang moves in one PR:

```
Systems/
  UI/
    Shared/
      OverlayTextureUtil.cs          # rename namespace optional: Dread.Systems.UI
      ImGuiTexture.cs                # UI-1: single 1x1 solid texture helper (merge duplicate MakeTexture)
      DreadImGuiTheme.cs             # UI-1: ColAccent, panel/label/button factories
      ImGuiInputCapture.cs           # UI-1: cursor lock + PlayerController input lock (from prompt)
    ImGui/
      DebugOverlay/                  # move existing partials
      ErrorReportingPromptSystem.cs  # presentation only; keep ErrorReportingConsent in ErrorReporting/
    Notifications/                   # future: toast queue, no ErrorReporting dependency
  Debug/
    DebugServerSystem.cs             # ADR-0013; pairs with UI/DebugOverlay, not inside UI/
  PsychoticBreak/
    PsychoticBreakOverlay.cs         # stays: gameplay effect, calls UI/Shared/OverlayTextureUtil
```

**Boundaries:**

| Concern | Home | Rationale |
|---------|------|-----------|
| IMGUI HUD + modals | `Systems/UI/ImGui/` | Same render stack, shared theme (UI-1) |
| uGUI fullscreen effects | Feature folders (`PsychoticBreak/`) | Episode-owned lifecycle; reuse `UI/Shared` textures only |
| TCP / MCP debug API | `Systems/Debug/` | ADR-0013; not drawn on screen |
| Consent / upload policy | `Systems/ErrorReporting/` | `ErrorReportingConsent`, queue, uploader stay domain-side |
| REPOConfig hacks | `Systems/Core/` | Soft dependency on MenuLib (01-core-review) |
| Live state for tools | `DreadRuntimeState` | Keep; UI reads, gameplay systems write (ADR-0016) |

### Notifications vs UI (investigation)

| Question | Finding |
|----------|---------|
| Does a notification system exist? | **No**: `rg -i notif` on `*.cs` returns nothing in-game. |
| Is error prompt a “notification”? | **No**: blocking modal with input lock; different UX and code path than toasts. |
| Should notifications live inside UI? | **Yes, when built**: use `Systems/UI/Notifications/` with a small API (`EnqueueToast`, duration, severity). Domain systems (tension, break, errors) publish **events or calls** into that API; do not render from `ErrorReporting/`. |
| Shared rendering? | Future toasts likely IMGUI or uGUI; either way consume **UI-1 theme** + `OverlayTextureUtil` for backgrounds. |
| Coupling today | Only coupling is **shared patterns** (duplicate `MakeTexture`, similar colors): not a notification module. |

## Issues

### [SEVERITY: CRITICAL] CI analyze globs omit nested `Systems/` paths

- **Location:** `.github/workflows/ci.yml` (analyze job); see [01-core-review.md](01-core-review.md)
- **Category:** maintainability
- **Description:** `Systems/DebugOverlay/`, `ErrorReportingPromptSystem.cs`, and `OverlayTextureUtil.cs` are not covered by `Systems/*.cs` flat globs. Same class of issue as Core.
- **Suggested fix:** Include `Systems/**/*.cs` in analyze (and `scripts/verify-dread.ps1` if extended).

### [SEVERITY: IMPORTANT] IMGUI textures not destroyed on `OnDestroy`

- **Location:** `DebugOverlay/DebugOverlayStyles.cs` (`_bgTex`, `_sepTex`); `ErrorReporting/ErrorReportingPromptSystem.cs` (`_overlayTex` … `_buttonPrimaryHoverTex`, six textures)
- **Category:** resource cleanup
- **Description:** Host objects use `DontDestroyOnLoad`; styles are created once per session. Textures are never `Destroy`ed when the component is destroyed (config toggle off rarely destroys host). Leak is session-long, not per-frame, but violates review criterion for resource cleanup and complicates hot-reload tests.
- **Suggested fix:**
  ```csharp
  private void OnDestroy()
  {
      DreadConfig.DebugOverlayEnabled.SettingChanged -= OnOverlayConfigChanged;
      if (_bgTex != null) { Destroy(_bgTex); _bgTex = null; }
      if (_sepTex != null) { Destroy(_sepTex); _sepTex = null; }
      _boxStyle = null; // force rebuild if re-enabled
  }
  ```
  Mirror in `ErrorReportingPromptSystem` for all `_`*Tex fields.

### [SEVERITY: IMPORTANT] Shared `GUIStyle` mutated inside `OnGUI` row loop

- **Location:** `DebugOverlay/DebugOverlayPanel.cs:62-64`
- **Category:** bug
- **Description:** `GUIStyle valueStyle = _valueStyle; valueStyle.normal.textColor = row.Color;` mutates the cached style’s normal state. Later rows (and header pass) can render with the last row’s color. IMGUI may also reuse the style reference across frames unpredictably.
- **Suggested fix:** Per-row color without mutating cache:
  ```csharp
  var content = new GUIContent(row.Right);
  var prev = _valueStyle.normal.textColor;
  _valueStyle.normal.textColor = row.Color;
  GUI.Label(rect, content, _valueStyle);
  _valueStyle.normal.textColor = prev;
  ```
  Or UI-1: `GUI.skin.customStyles` / dedicated style per color tier.

### [SEVERITY: IMPORTANT] Duplicated Harmony patch counting (hot when visible)

- **Location:** `DebugOverlay/DebugOverlayPanel.cs:176-202`; `DebugServerSystem.cs:666-695`
- **Category:** maintainability / performance
- **Description:** Identical `Harmony.GetAllPatchedMethods()` loops (~30 lines). Overlay refreshes every 0.5s while visible; debug server uses cached `DreadRuntimeState.DreadPatchCount` when set, else repeats full scan on `verify` / cold paths.
- **Suggested fix:** Extract `HarmonyPatchStats.CountOwnedPatches(string ownerGuid)` in `Systems/Core/HarmonyPatchCompat.cs` (or `Systems/UI/Shared/`). Single implementation; overlay and server call it.

### [SEVERITY: IMPORTANT] `ErrorReportingPromptSystem` is UI but lives under `ErrorReporting/`

- **Location:** `Systems/ErrorReporting/ErrorReportingPromptSystem.cs`; registry id `error-reporting-prompt` in `SystemOrderGroup.Core`
- **Category:** structure
- **Description:** 436 lines of IMGUI layout, styles, cursor capture, and input lock: no HTTP, queue, or payload logic. Placing it in ErrorReporting invites future “notification” and upload code in the same folder. Registry groups it with core gameplay systems, not debug.
- **Suggested fix:** Move to `Systems/UI/ImGui/ErrorReportingPromptSystem.cs` (namespace can stay `Dread.Systems` initially). Keep `ErrorReportingConsent.cs` in ErrorReporting. Optional: `SystemOrderGroup.Ui` between Core and Debug.

### [SEVERITY: IMPORTANT] `OverlayTextureUtil` orphaned at `Systems/` root

- **Location:** `Systems/OverlayTextureUtil.cs`
- **Category:** structure
- **Description:** Only caller is `PsychoticBreakOverlay.cs`. Name suggests generic UI utility but file sits beside audio/tension systems. Agents searching `Systems/UI/` will miss it.
- **Suggested fix:** Move to `Systems/UI/Shared/OverlayTextureUtil.cs` (internal static, unchanged API). Document in `domain.md` ARCH-1.

### [SEVERITY: IMPORTANT] Error prompt bypasses `OverlayTextureUtil` (Proton/Linux)

- **Location:** `ErrorReportingPromptSystem.cs:406-411` (`MakeTexture`); contrast `OverlayTextureUtil.cs:12-20`
- **Category:** bug / portability
- **Description:** Prompt uses `new Texture2D(1, 1)` + `Apply()` only. Debug overlay uses the same simple path in `DebugOverlayStyles.MakeTexture`. Psychotic break vignette uses format fallback + `SupportsTextureFormat` guard. On Proton/Linux, unsupported formats can throw (documented in `OverlayTextureUtil` header).
- **Suggested fix:** Route all IMGUI background textures through `OverlayTextureUtil.CreateSolid` or shared `ImGuiTexture.CreateSolid(Color)`.

### [SEVERITY: IMPORTANT] PERF-2 gap: FPS sampling runs when HUD is F10-hidden

- **Location:** `DebugOverlay/DebugOverlaySystem.cs:36-44`, `57-77`
- **Category:** performance
- **Description:** When `DebugOverlayEnabled` is true but the player toggles F10 off, `Update` still calls `SampleFrameStats()` every frame. Checklist `docs/agents/overlay-perf-checklist.md` case B expects patch-count reflection to stop; FPS smoothing still runs (not documented there). `OnGUI` early-outs correctly.
- **Suggested fix:** Gate `SampleFrameStats()` on `IsOverlayVisible()` or a “was visible recently” debounce if instant FPS on open is required.

### [SEVERITY: IMPORTANT] `DebugServerSystem.cs` monolith at `Systems/` root

- **Location:** `Systems/DebugServerSystem.cs` (1014 lines)
- **Category:** maintainability
- **Description:** Mixes TCP threading, JSON protocol, config mirror, Harmony enumeration, `FindObjectsOfType` game probes, and verify checks. Hard to navigate; overlaps debug overlay concerns (`CaptureRuntimeState`, patch counts).
- **Suggested fix:** Split under `Systems/Debug/`: `DebugServerSystem.cs` (lifecycle + queue), `DebugServerCommands.cs` (switch), `DebugServerDtos.cs` (serializable types). Keep ADR-0013 protocol in one doc.

### [SEVERITY: NICE_TO_HAVE] Duplicate IMGUI theme (UI-1 not started)

- **Location:** `DebugOverlayStyles.cs` colors; `ErrorReportingPromptSystem.cs:36-44` (near-identical `ColAccent`, `ColDim`, `ColBody`)
- **Category:** maintainability
- **Description:** ROADMAP UI-1 explicitly tracks unified kit. Two copies will drift (font sizes already differ: 15 vs 22 brand).
- **Suggested fix:** Implement `DreadImGuiTheme` per ROADMAP; migrate overlay + prompt as first consumers.

### [SEVERITY: NICE_TO_HAVE] Duplicate player input-lock reflection

- **Location:** `ErrorReportingPromptSystem.cs:316-351`; psychotic break lockdown in `PsychoticBreakPlayerLockdown.cs`
- **Category:** maintainability
- **Description:** Same Traverse field names (`inputLocked`, `interactDisabled`, …) in two UI/gameplay flows.
- **Suggested fix:** After UI-1 `ImGuiInputCapture`, call shared helper (could wrap `PlayerControllerCompat` extension).

### [SEVERITY: NICE_TO_HAVE] `domain.md` ARCH-1 map incomplete for UI surface

- **Location:** `docs/agents/domain.md` (runtime file map)
- **Category:** documentation
- **Description:** Lists `DebugOverlay/` only. Omits `ErrorReportingPromptSystem`, `OverlayTextureUtil`, flat `DebugServerSystem`, and planned UI folder.
- **Suggested fix:** Add rows for Debug server, UI/prompt, shared texture util; link ROADMAP UI-1.

### [SEVERITY: NICE_TO_HAVE] `CONTEXT.md` debug overlay wording stale

- **Location:** `CONTEXT.md` (debug overlay section)
- **Category:** documentation
- **Description:** States overlay is “not wired on all branches yet”; registry and `DebugOverlaySystem` are present on this branch.
- **Suggested fix:** Align with shipped F10 HUD + config `DebugOverlayEnabled`.

### [SEVERITY: NICE_TO_HAVE] IMGUI z-order undocumented if multiple surfaces visible

- **Location:** `ErrorReportingPromptSystem.cs:173-174` (`GUI.depth = 10000`); `DebugOverlayPanel.OnGUI` (default depth)
- **Category:** design
- **Description:** Prompt should win over debug HUD if both render (edge case: debug overlay on + prompt pending). No comment or guard preventing simultaneous display.
- **Suggested fix:** Document stacking; optionally skip debug `OnGUI` while `ErrorReportingPromptSystem` is `Visible`.

### [SEVERITY: NICE_TO_HAVE] `CountDreadPatches` in overlay runs on menu levels

- **Location:** `DebugOverlay/DebugOverlaySystem.cs:49-54` (refresh gated by `IsOverlayVisible()` which excludes menu); `DebugOverlayPanel.cs:176+`
- **Category:** performance
- **Description:** When visible in-level, full Harmony scan every 0.5s allocates enumerators. Acceptable for debug; note under PERF-1.
- **Suggested fix:** Cache in `DreadRuntimeState` from a single writer; overlay reads only.

### [SEVERITY: STYLE] `DebugServerSystem` uses `FindObjectOfType` in commands

- **Location:** `DebugServerSystem.cs:410, 645, 656-662, 717, 756`
- **Category:** performance
- **Description:** Not Update-hot; acceptable for MCP. Prefer registry or cached refs if verify runs in a loop from agents.
- **Suggested fix:** Low priority; document as debug-only cost.

### [SEVERITY: STYLE] Psychotic break uGUI uses reflection while project references `UnityEngine.UI`

- **Location:** `PsychoticBreakOverlay.cs:55-124`
- **Category:** consistency
- **Description:** `Dread.csproj` references UI assemblies; overlay still resolves `RawImage`/`Canvas` via reflection (stub-build heritage). Works but harder to read than compile-time components on full game builds.
- **Suggested fix:** `#if` or dual build targets per ARCH-2 inventory **reduce** list; out of UI-1 scope.

## Positive Patterns

- **Debug overlay partial split:** `System` / `Panel` / `Styles` keeps `OnGUI` readable (ARCH-1 precedent).
- **`GUIContent` stub workaround:** `EmptyContent` instead of `GUIContent.none` avoids `MissingMethodException` on game Unity (documented in panel).
- **PERF-2 config gate:** `enabled = false` when `DebugOverlayEnabled` is false; `GuardOverlayEnabled` logs regression if wiring breaks.
- **`DreadRuntimeState` contract:** Gameplay systems publish; overlay and `get_runtime_state` consume without cross-system references (ADR-0016).
- **Menu suppression:** `IsOverlayVisible()` uses `SemiFunc.MenuLevel()`: consistent with prompt activation rules.
- **Psychotic break overlay cleanup:** `CleanupOverlay` destroys root GO and vignette texture (contrast with IMGUI hosts).
- **`OverlayTextureUtil` Proton guard:** Format iteration + safe `SupportsTextureFormat` try/catch: good shared primitive for UI-1.
- **Debug server threading model:** Background accept + main-thread `Update` drain matches `ErrorReporterSystem` pattern (ADR-0013).
- **Registry extensibility:** Adding overlay/prompt via `DreadSystemRegistry` avoids `Plugin.cs` spawn list edits (ADR-0016).

## Recommended Agent Prompts

**P0 (CI):**  
> Extend CI analyze globs to `Systems/**/*.cs` so DebugOverlay, ErrorReportingPrompt, and OverlayTextureUtil get the same grep rules as root `Systems/*.cs`. See 01-core-review.

**P1 (UI-1 foundation):**  
> Add `Systems/UI/Shared/ImGuiTexture.cs` using `OverlayTextureUtil.CreateSolid`. Add `DreadImGuiTheme` with accent/dim/body colors and box/label/button style factories. Migrate `DebugOverlayStyles` and `ErrorReportingPromptSystem.EnsureStyles` to use it without behavior change.

**P1 (patch count DRY):**  
> Extract `HarmonyPatchCompat.CountPatchesOwnedBy(string guid)` from duplicated overlay/server loops. Wire `DebugOverlaySystem` refresh and `DebugServerSystem.GetDreadPatchCount` to it.

**P1 (overlay style bug):**  
> Fix `DebugOverlayPanel` row coloring without mutating cached `_valueStyle.normal.textColor`. Add a one-line comment why `GUIContent.none` is avoided.

**P2 (folder governance):**  
> Move `ErrorReportingPromptSystem.cs` to `Systems/UI/ImGui/`, `OverlayTextureUtil.cs` to `Systems/UI/Shared/`, and `DebugServerSystem.cs` to `Systems/Debug/`. Update `domain.md` ARCH-1 and `DreadSystemRegistry` only if paths change public docs. Do not move consent/uploader types.

**P2 (texture cleanup):**  
> Destroy IMGUI helper textures in `OnDestroy` for debug overlay and error prompt hosts.

**P2 (PERF-2 follow-up):**  
> Gate `SampleFrameStats()` when F10 hides overlay; update `docs/agents/overlay-perf-checklist.md` case B if intentional.

**P3 (notifications scaffold):**  
> When adding toasts, create `Systems/UI/Notifications/` with `INotificationPresenter` and a single `NotificationHost` MonoBehaviour registered in `DreadSystemRegistry` under `SystemOrderGroup.Core` or new `Ui`. No imports from `ErrorReporting` except optional “report failed” messages via interface.

**P3 (DBG-5):**  
> After UI-1, add overlay section registry API so features register rows without editing `DebugOverlayPanel.BuildRows`.

## Cross-references

| Doc | Relevance |
|-----|-----------|
| [01-core-review.md](01-core-review.md) | CI globs; REPOConfig UI in Core; do not re-review |
| [ADR-0013](../adr/0013-debug-server.md) | Debug server protocol; separate from IMGUI overlay |
| [ADR-0016](../adr/0016-arch-3-extension-model.md) | Registry, `DreadRuntimeState`, debug row spawn rules |
| [ADR-0011](../adr/0011-psychotic-break-system.md) | Canvas overlay episode lifecycle |
| [domain.md](../agents/domain.md) | Debug overlay + F10; REPOConfig slider compat |
| [overlay-perf-checklist.md](../agents/overlay-perf-checklist.md) | PERF-2 manual matrix |
| [ROADMAP.md](../ROADMAP.md) | UI-1, DBG-3/5, Phase 4 player-facing UI |
| [specs/004-err-2-default-on-prompt/](../../specs/004-err-2-default-on-prompt/) | Error reporting prompt behavior |

### Caller map (rg, 2026-06-01)

| Symbol | Callers / consumers |
|--------|---------------------|
| `OverlayTextureUtil.*` | `PsychoticBreakOverlay.cs` only |
| `DebugOverlaySystem` | `DreadSystemRegistry`, `DebugServerSystem` verify/runtime_state |
| `ErrorReportingPromptSystem` | `DreadSystemRegistry`; gates via `ErrorReportingConsent` |
| `ErrorReportingConsent` | `ErrorReporterSystem`, upload paths (not UI) |
| `DreadRuntimeState` | `TensionSystem`, `PsychoticBreakSystem`, `AudioDreadSystem`, `DebugOverlaySystem`, `DebugServerSystem` |
| `DebugServerSystem` | `dread-mcp-server` tools (out of repo); config `DebugServerEnabled` |

### Severity summary

| Severity | Count |
|----------|------:|
| CRITICAL | 1 |
| IMPORTANT | 8 |
| NICE_TO_HAVE | 6 |
| STYLE | 2 |

**Top 3 concerns:** (1) no `Systems/UI/` governance while IMGUI code duplicates and scatters; (2) misplaced/error-domain coupling for `ErrorReportingPromptSystem` and no notification module yet; (3) resource/style bugs (texture leak, shared `GUIStyle` mutation) plus duplicated Harmony scan.
