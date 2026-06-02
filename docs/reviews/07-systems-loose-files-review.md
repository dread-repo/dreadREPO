# Code Review: Systems/ loose files (root)

**Reviewed:** 2026-06-02  
**Scope:** All `.cs` files directly under `Systems/` (not `Core/`, `Patches/`, `PsychoticBreak/`, `ErrorReporting/`, `DebugOverlay/`). Cross-refs only for overlap with prior reviews.  
**Files:** `AudioClipLoader.cs`, `AudioDreadSystem.cs`, `AudioPlayUtil.cs`, `DebugServerSystem.cs`, `DreadRuntimeState.cs`, `DreadSystemInitializer.cs`, `DreadSystemRegistry.cs`, `EnemyScanCache.cs`, `ErrorReportJson.cs`, `ErrorReportTypes.cs`, `FlashlightStateTracker.cs`, `LoggingService.cs`, `MonsterOverhaulSystem.cs`, `OverlayTextureUtil.cs`, `PluginDependencyResolver.cs`, `TensionSystem.cs`, `TestCrashSystem.cs`

**Prior reviews (not re-reviewed in depth):** [01-core-review.md](01-core-review.md) through [06-error-reporting-review.md](06-error-reporting-review.md)

## Executive Summary

ARCH-1 split the largest god files into subfolders, but **17 files remain at `Systems/` root**, mixing bootstrapping, audio, tension, debug TCP, error JSON, and shared utilities. Nothing in this set is unused stub code: all types have live callers. The user concern is valid: **structure is half-finished**, there is **no enforced placement policy** (now documented in [systems-folder-governance.md](../agents/systems-folder-governance.md)), and **duplicate enemy scans** undermine `EnemyScanCache`.

**Live code, messy map:** registry/initializer/runtime state belong together; audio/tension/monster belong in feature folders; `DebugServerSystem` (~1k lines) should move under `Debug/` and likely split; error JSON/types should sit with `ErrorReporting/`; `FlashlightStateTracker` belongs with psychotic break; `OverlayTextureUtil` belongs in UI shared (per [02-ui-review.md](02-ui-review.md)).

**Review outcome:** ❌ **ISSUES** — no ship-blocking bugs in the loose files alone, but **IMPORTANT** maintainability and performance debt (duplicate `FindObjectsOfType`, dead API, 1k-line debug server, incomplete ARCH-1). Agent governance and a phased folder map should be treated as **P1** follow-up to ARCH-1.

## File Structure Assessment

### Why root is a problem

| Symptom | Impact |
|---------|--------|
| 17 files at one depth | Agents default to creating more root files (see git history before ARCH-1) |
| Mixed concerns | Boot, audio, tension, debug, JSON, and psychotic-break helper in one directory |
| `domain.md` ARCH-1 says "Other systems: `Systems/*.cs` (flat)" | Encourages status quo; contradicts finished ARCH-1 intent in ROADMAP |
| Nested folders skip CI style grep | Documented in 01-core; root loose files **are** grep-checked (`Systems/*.cs`) |

### Proposed folder map

Target layout (namespace `Dread.Systems` unchanged; moves can be one PR per band):

```
Systems/
  Bootstrap/
    DreadSystemInitializer.cs
    DreadSystemRegistry.cs
    PluginDependencyResolver.cs      # called from Plugin.Awake before anything else
  Runtime/
    DreadRuntimeState.cs
  Infrastructure/
    LoggingService.cs
  Audio/
    AudioClipLoader.cs
    AudioDreadSystem.cs
    AudioPlayUtil.cs
    MonsterOverhaulSystem.cs         # includes DreadAudioTweaked marker (or Audio/Monster/)
  Tension/
    TensionSystem.cs
  Scan/
    EnemyScanCache.cs
  Debug/
    DebugServerSystem.cs             # split CommandDispatch / TcpServer later
    TestCrashSystem.cs
  ErrorReporting/                    # merge loose JSON (06-error-reporting-review)
    ErrorReportJson.cs
    ErrorReportTypes.cs
    … (existing partials)
  UI/
    Shared/
      OverlayTextureUtil.cs          # per 02-ui-review
  PsychoticBreak/
    FlashlightStateTracker.cs        # only caller: PsychoticBreakPlayerLockdown
  Core/ Patches/ DebugOverlay/ …     # unchanged
```

| Current file | Proposed home | Rationale |
|--------------|---------------|-----------|
| `PluginDependencyResolver.cs` | `Bootstrap/` | Process-wide, pre-system; not gameplay |
| `DreadSystemInitializer.cs` | `Bootstrap/` | ARCH-3 boot (ADR-0016) |
| `DreadSystemRegistry.cs` | `Bootstrap/` | Extension manifest |
| `DreadRuntimeState.cs` | `Runtime/` | Cross-cutting read model for HUD/MCP |
| `LoggingService.cs` | `Infrastructure/` | Used by entire DLL |
| `AudioClipLoader.cs` | `Audio/` | NVorbis + UWR loading (ADR-0006/0007) |
| `AudioDreadSystem.cs` | `Audio/` | Ambient dread playback |
| `AudioPlayUtil.cs` | `Audio/` | Pitch-aware `Destroy` delay |
| `MonsterOverhaulSystem.cs` | `Audio/` or `Audio/Monster/` | Enemy audio tweak loop |
| `TensionSystem.cs` | `Tension/` | Adrenaline, panic sprint, fake footsteps |
| `EnemyScanCache.cs` | `Scan/` | Shared 0.5s enemy list |
| `DebugServerSystem.cs` | `Debug/` | ADR-0013 TCP tooling |
| `TestCrashSystem.cs` | `Debug/` | `SystemOrderGroup.Debug` |
| `ErrorReportJson.cs` | `ErrorReporting/` | Same feature as uploader (06) |
| `ErrorReportTypes.cs` | `ErrorReporting/` | DTOs + xUnit tests |
| `OverlayTextureUtil.cs` | `UI/Shared/` | Proton-safe textures (02) |
| `FlashlightStateTracker.cs` | `PsychoticBreak/` | Episode lockdown only |

### Agent governance

Concrete placement rules for contributors and agents: **[docs/agents/systems-folder-governance.md](../agents/systems-folder-governance.md)**. Update `domain.md` ARCH-1 table when the first move PR lands.

## Dead code inventory

| Symbol / file | Evidence | Disposition |
|---------------|----------|-------------|
| `EnemyScanCache.NearestDistance` | `grep` — definition only in `EnemyScanCache.cs:23` | **Remove** or wire `TensionSystem.FindNearestEnemyDist` to use it |
| `EnemyScanCache` itself | Callers: `PsychoticBreakTrigger`, `PsychoticBreakSystem`, `ErrorReportPayloadCapture` | **Keep** |
| `FlashlightStateTracker` | Callers: `PsychoticBreakPlayerLockdown` only | **Keep**, move folder |
| `OverlayTextureUtil` | Caller: `PsychoticBreakOverlay.CreateVignette` | **Keep**, move folder |
| `AudioPlayUtil` | Callers: `AudioDreadSystem`, `TensionSystem`, `PsychoticBreakAudio` | **Keep** |
| `TestCrashSystem` | Registry debug row; `DebugServerSystem.TriggerForDebug`; config button | **Keep** (debug-only) |
| `PluginDependencyResolver` | `Plugin.cs:32` only | **Keep** (NVorbis etc. beside DLL) |
| `ErrorReportJson` / types | Reporter, uploader, xUnit tests | **Keep**, relocate |
| `PlayerControllerCompat.GetStamina` | Not in this scope | See [01-core-review.md](01-core-review.md) |

No loose file is **entirely** unreferenced. The only confirmed dead **method** in this scope is `NearestDistance`.

## Per-file review

### `PluginDependencyResolver.cs` (36 lines)

| | |
|--|--|
| **Callers** | `Plugin.Awake` → `Register()` once |
| **Folder** | `Bootstrap/` |
| **Quality** | Correct `AssemblyResolve` for plugin-dir DLLs. Empty `catch` on load failure returns null (silent). Null-forgiving `Location!` (line 16). |

### `DreadSystemRegistry.cs` (90 lines)

| | |
|--|--|
| **Callers** | `DreadSystemInitializer` only |
| **Folder** | `Bootstrap/` |
| **Quality** | Clear ARCH-3 manifest; eight systems, Core then Debug. **No `IsEnabled` predicates** — debug hosts always spawn (self-disable in `Start` where applicable). `test-crash` always registered (acceptable for debug). |

### `DreadSystemInitializer.cs` (108 lines)

| | |
|--|--|
| **Callers** | `Plugin.OnSceneLoaded` until success |
| **Folder** | `Bootstrap/` |
| **Quality** | UI defer gate is sound. Per-system try/catch fail-safe (ADR-0016). `RepoConfigCompat.TryApply` after spawn matches ADR. |

### `DreadRuntimeState.cs` (32 lines)

| | |
|--|--|
| **Callers** | `TensionSystem`, `AudioDreadSystem`, `PsychoticBreakSystem`, `DebugOverlayPanel`, `DebugServerSystem` (`get_runtime_state`) |
| **Folder** | `Runtime/` |
| **Quality** | Good ADR-0016 contract. Writers use `internal set`; public getters — intentional for MCP JSON. `DreadPatchCount` written in `DebugOverlaySystem` only. |

### `LoggingService.cs` (102 lines)

| | |
|--|--|
| **Callers** | Widespread (Plugin, Config, all systems, patches, Core) |
| **Folder** | `Infrastructure/` |
| **Quality** | `LogWarning` requires `LogLevel.Debug` (lines 68-71), same threshold as `LogInfo` — warnings hidden when level is `Error` only. Large ASCII art at boot (operator preference). |

### `AudioClipLoader.cs` (194 lines)

| | |
|--|--|
| **Callers** | `AudioDreadSystem`, `TensionSystem`, `PsychoticBreakAudio`; `UnityWebRequestCompat` from Core |
| **Folder** | `Audio/` |
| **Quality** | NVorbis-first + UWR fallback matches ADR. In-memory cache never evicts (scene load clears via `TensionSystem.OnSceneLoaded`). `ToFileUri` Linux `Z:` prefix hack (lines 100-101). `GetDownloadHandlerError` reflection for stub builds — used, not dead. |

### `AudioDreadSystem.cs` (151 lines)

| | |
|--|--|
| **Callers** | Registry `audio-dread`; `DebugServerSystem.CountActiveSystems` |
| **Folder** | `Audio/` |
| **Quality** | Subscribes/unsubscribes `sceneLoaded`; stops coroutines on destroy. Spawns `GameObject` per sound (GC); acceptable at 60-180s cadence. Updates `DreadRuntimeState` in `Update` + loop. |

### `AudioPlayUtil.cs` (12 lines)

| | |
|--|--|
| **Callers** | `AudioDreadSystem`, `TensionSystem`, `PsychoticBreakAudio` |
| **Folder** | `Audio/` |
| **Quality** | Small, correct pitch-aware lifetime (AUDIO-1). `internal` — good. |

### `MonsterOverhaulSystem.cs` (86 lines)

| | |
|--|--|
| **Callers** | Registry `monster-overhaul` |
| **Folder** | `Audio/` (or `Audio/Monster/`) |
| **Quality** | **`FindObjectsOfType<EnemyHealth>()` every 4s** (line 46) bypasses `EnemyScanCache`. `IsSourcePlaying` reflection on `AudioSource` (lines 58-68). `DreadAudioTweaked` marker in same file — fine until split. `public` class vs `internal` registry types — inconsistent. |

### `TensionSystem.cs` (318 lines)

| | |
|--|--|
| **Callers** | Registry `tension` |
| **Folder** | `Tension/` |
| **Quality** | **`FindNearestEnemyDist` uses raw `FindObjectsOfType` every 0.5s** (lines 234-239) despite `EnemyScanCache` at same interval. Harmony `Traverse` for panic sprint. Restores drain/sprint on destroy and scene load. Uses `AudioClipLoader` + `AudioPlayUtil` well. |

### `EnemyScanCache.cs` (89 lines)

| | |
|--|--|
| **Callers** | `PsychoticBreakTrigger`, `PsychoticBreakSystem.Invalidate`, `ErrorReportPayloadCapture` |
| **Folder** | `Scan/` |
| **Quality** | Single `FindObjectsOfType` per 0.5s with in-place trim — good pattern. **`NearestDistance` unused.** Does not filter by alive (compat valid filter only). |

### `DebugServerSystem.cs` (1014 lines)

| | |
|--|--|
| **Callers** | Registry `debug-server`; MCP/dread-mcp-server (ADR-0013) |
| **Folder** | `Debug/` (+ future split) |
| **Quality** | Background thread accepts TCP; commands run on main thread via queue — correct Unity pattern. **`CaptureState` duplicates enemy scan** (line 408) and player HP via reflection (lines 413-414) instead of Core + cache. Duplicates Harmony patch counting vs overlay (`GetDreadPatchCount`). Large surface: config R/W, verify, psychotic break force, test crash. Many empty catches on shutdown/network. |

### `TestCrashSystem.cs` (117 lines)

| | |
|--|--|
| **Callers** | Registry; `DreadConfig.TestCrashButton`; `DebugServerSystem.trigger_test_crash`; `ErrorReportLogQueue` filters stack |
| **Folder** | `Debug/` |
| **Quality** | Deferred crash on main thread; sync report coroutine before `Kill()` (ADR-0012). `FindObjectOfType<ErrorReporterSystem>()` — acceptable for test harness. |

### `ErrorReportTypes.cs` / `ErrorReportJson.cs`

| | |
|--|--|
| **Callers** | `ErrorReporterSystem`, `ErrorReportUploader`, `tests/Dread.ErrorReportJson.Tests` |
| **Folder** | `ErrorReporting/` (see [06-error-reporting-review.md](06-error-reporting-review.md)) |
| **Quality** | Manual JSON avoids `JsonUtility` array bug (ADR-0015). Escaping is thorough. Types shared with test project — keep `internal` + InternalsVisibleTo or move tests namespace together. |

### `OverlayTextureUtil.cs` (90 lines)

| | |
|--|--|
| **Callers** | `PsychoticBreakOverlay` only |
| **Folder** | `UI/Shared/` per [02-ui-review.md](02-ui-review.md) |
| **Quality** | Proton-safe format probing — good. Not used by error prompt (duplicate `MakeTexture` there). |

### `FlashlightStateTracker.cs` (9 lines)

| | |
|--|--|
| **Callers** | `PsychoticBreakPlayerLockdown` add/get component |
| **Folder** | `PsychoticBreak/` |
| **Quality** | Minimal marker; `public` type in global Systems namespace — move reduces confusion. |

## Issues

### [SEVERITY: CRITICAL] CI analyze still omits nested `Systems/**` (carried)

- **Location:** `.github/workflows/ci.yml` lines 170-198
- **Category:** maintainability
- **Description:** Only `Systems/*.cs` at root is scanned. After moving loose files into subfolders, they **lose** grep coverage unless globs expand to `Systems/**/*.cs`.
- **Suggested fix:** Expand CI and `scripts/verify-dread.ps1` to `Systems/**/*.cs` before or with folder moves.

### [SEVERITY: IMPORTANT] Duplicate `FindObjectsOfType<EnemyHealth>` scans

- **Location:** `TensionSystem.cs:234-239`, `MonsterOverhaulSystem.cs:46`, `DebugServerSystem.cs:408` vs `EnemyScanCache.cs:64`
- **Category:** performance / DRY
- **Description:** `EnemyScanCache` exists to centralize 0.5s scans; tension repeats the same work; monster audio scans every 4s independently; debug server scans on each `get_state`.
- **Suggested fix:** Use `EnemyScanCache.GetEnemies()` / `NearestDistance` (or extend cache for alive-only nearest). Invalidate on scene load in tension/monster if needed.

### [SEVERITY: IMPORTANT] Dead API: `EnemyScanCache.NearestDistance`

- **Location:** `EnemyScanCache.cs:23-38`
- **Category:** dead-code
- **Description:** No callers in repository.
- **Suggested fix:** Wire `TensionSystem.FindNearestEnemyDist` to it, or delete the method.

### [SEVERITY: IMPORTANT] ARCH-1 incomplete: 17 root files remain

- **Location:** `Systems/*.cs` (flat)
- **Category:** structure
- **Description:** ROADMAP marks ARCH-1 done (#201) but half the surface area still flat; agents lack rules until [systems-folder-governance.md](../agents/systems-folder-governance.md).
- **Suggested fix:** Phased moves per proposed folder map; update `domain.md` ARCH-1 table.

### [SEVERITY: IMPORTANT] `DebugServerSystem` god file (~1k lines)

- **Location:** `DebugServerSystem.cs`
- **Category:** maintainability
- **Description:** TCP loop, command dispatch, config mirror, verify checks, and psychotic break/test hooks in one type.
- **Suggested fix:** Move to `Systems/Debug/`; split `DebugServerCommands.cs` + `DebugServerTcp.cs` when touching next.

### [SEVERITY: IMPORTANT] `FlashlightStateTracker` wrong package boundary

- **Location:** `FlashlightStateTracker.cs` at root
- **Category:** structure
- **Description:** Only psychotic break uses it; root placement suggests shared systems API.
- **Suggested fix:** Move to `PsychoticBreak/` (no behavior change).

### [SEVERITY: NICE_TO_HAVE] `LoggingService.LogWarning` gated at Debug level

- **Location:** `LoggingService.cs:68-71`
- **Category:** design
- **Description:** Operators at `LogLevel.Error` do not see warnings (including audio load failures).
- **Suggested fix:** Gate warnings at `LogLevel.Error` like `LogError`.

### [SEVERITY: NICE_TO_HAVE] `public` gameplay systems vs `internal` infrastructure

- **Location:** `AudioDreadSystem`, `MonsterOverhaulSystem`, `TensionSystem`, `TestCrashSystem`, `DebugServerSystem` vs `internal` helpers
- **Category:** naming
- **Description:** No external assembly consumers; `public` widens surface accidentally.
- **Suggested fix:** `internal class` unless Thunderstore API needs public (ARCH-4).

### [SEVERITY: NICE_TO_HAVE] `AudioClipLoader` cache unbounded for session

- **Location:** `AudioClipLoader.cs:17`, cleared only from `TensionSystem.OnSceneLoaded`
- **Category:** performance
- **Description:** All loaded clips stay in static dictionary until scene load.
- **Suggested fix:** Document as intentional or clear on `Plugin` destroy / level unload.

### [SEVERITY: STYLE] Null-forgiving on plugin paths

- **Location:** `AudioClipLoader.cs:22`, `PluginDependencyResolver.cs:16`
- **Category:** style
- **Description:** Would fail CI if file lived only under nested path without glob fix; use null checks for `Assembly.Location`.

## Positive Patterns

- **ARCH-3 boot chain:** `Plugin` → scene load → `DreadSystemInitializer` → `DreadSystemRegistry` is clear and verified by scripts.
- **`AudioClipLoader`:** NVorbis primary, UWR gated by `UnityWebRequestCompat`, shared by three features.
- **`AudioPlayUtil`:** Single place for pitch-aware `Destroy` delay (AUDIO-1).
- **`EnemyScanCache`:** In-place filter trim avoids extra allocations; `Invalidate()` hook from psychotic break.
- **`DreadRuntimeState`:** Thin snapshot decouples HUD/MCP from feature internals.
- **`TestCrashSystem`:** Safe main-thread crash + optional report before exit (ERR testing).
- **`OverlayTextureUtil`:** Defensive texture creation for Linux/Proton.

## Recommended Agent Prompts

**P0 (CI + moves):**  
> Expand CI analyze globs to `Systems/**/*.cs`. Then move `ErrorReportJson.cs` + `ErrorReportTypes.cs` into `ErrorReporting/` in one PR; run `dotnet build` and error JSON tests.

**P1 (scan consolidation):**  
> Refactor `TensionSystem.FindNearestEnemyDist` to use `EnemyScanCache` (implement or use `NearestDistance`). Switch `MonsterOverhaulSystem` to cache-backed enumeration. Remove dead `NearestDistance` if redundant.

**P1 (folder hygiene):**  
> Create `Systems/Bootstrap/`, `Audio/`, `Tension/`, `Scan/`, `Debug/`, `Runtime/`, `Infrastructure/` and move files per [07-systems-loose-files-review.md](07-systems-loose-files-review.md). Update `domain.md` ARCH-1. No namespace change.

**P2 (debug server):**  
> Move `DebugServerSystem` to `Systems/Debug/`; extract command handlers to partial or second file; use `EnemyHealthCompat` + `EnemyScanCache` in `CaptureState`.

**P2 (governance):**  
> Link [systems-folder-governance.md](../agents/systems-folder-governance.md) from `AGENTS.md` and `docs/agents/README.md`.

## Cross-references

| Doc | Relevance |
|-----|-----------|
| [systems-folder-governance.md](../agents/systems-folder-governance.md) | Agent placement rules (new) |
| [domain.md](../agents/domain.md) | ARCH-1 flat map — needs update after moves |
| [ADR-0016](../adr/0016-arch-3-extension-model.md) | Registry, `DreadRuntimeState`, boot order |
| [ADR-0013](../adr/0013-debug-server.md) | Debug TCP server |
| [02-ui-review.md](02-ui-review.md) | `OverlayTextureUtil` → `UI/Shared` |
| [06-error-reporting-review.md](06-error-reporting-review.md) | JSON/types placement |
| [01-core-review.md](01-core-review.md) | Compat layer; debug server should use Core for player/enemy reads |
| [ROADMAP.md](../ROADMAP.md) | ARCH-1 done; UI-1 / PERF-1 |

### Caller map (grep, 2026-06-02)

| File | Callers (representative) |
|------|---------------------------|
| `PluginDependencyResolver` | `Plugin.cs` |
| `DreadSystemInitializer` | `Plugin.cs` |
| `DreadSystemRegistry` | `DreadSystemInitializer.cs` |
| `DreadRuntimeState` | `TensionSystem`, `AudioDreadSystem`, `PsychoticBreakSystem`, `DebugOverlay/*`, `DebugServerSystem` |
| `LoggingService` | `Plugin`, `Config/DreadConfig`, most `Systems/**` |
| `AudioClipLoader` | `AudioDreadSystem`, `TensionSystem`, `PsychoticBreakAudio` |
| `AudioPlayUtil` | `AudioDreadSystem`, `TensionSystem`, `PsychoticBreakAudio` |
| `EnemyScanCache` | `PsychoticBreakTrigger`, `PsychoticBreakSystem`, `ErrorReportPayloadCapture` |
| `OverlayTextureUtil` | `PsychoticBreakOverlay` |
| `FlashlightStateTracker` | `PsychoticBreakPlayerLockdown` |
| `ErrorReportJson` / types | `ErrorReporterSystem`, `ErrorReportUploader`, xUnit tests |
| `TestCrashSystem` | Registry, `DreadConfig`, `DebugServerSystem`, log queue filter |

## Review outcome

❌ **ISSUES** — consolidate enemy scans, delete or wire dead `NearestDistance`, execute proposed folder map with CI glob fix, and adopt [systems-folder-governance.md](../agents/systems-folder-governance.md) for agent workflows.
