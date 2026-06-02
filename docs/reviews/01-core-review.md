# Code Review: Systems/Core

**Reviewed:** 2026-06-01  
**Scope:** `Systems/Core/` and integration (callers, `Plugin.cs`, `DreadSystemInitializer`, CI)  
**Files:** `EnemyHealthCompat.cs`, `HarmonyPatchCompat.cs`, `PlayerControllerCompat.cs`, `PlayerTumbleCompat.cs`, `RepoConfigCompat.cs`, `RepoConfigSliderLabelCompat.cs`, `UnityWebRequestCompat.cs`

## Executive Summary

`Systems/Core/` is a coherent **version-tolerance and soft-dependency layer** (ADR-0016, ERR-2): reflection/Harmony helpers that keep feature code off brittle game field names. The split is justified and callers are mostly consistent. Main risks are **fail-open compat semantics** (health unread, master-client probe failure), **process-wide tumble lock state** in `PlayerTumbleCompat`, **global REPOConfig UI patches**, and **CI analyze globs that skip nested `Systems/**` folders** so Core never gets grep checks. One confirmed dead API (`GetStamina`). Documentation map in `domain.md` ARCH-1 lags ADR-0016 on the Core folder.

## File Structure Assessment

**"Core" is the right boundary** for this code. ADR-0016 defines `Dread.Systems.Core` as static compat for game types, Harmony guards, REPOConfig, and UWR probing. That is distinct from:

| Concern | Better home | Notes |
|---------|-------------|--------|
| Harmony patch IL | `Systems/Patches/` | Already separate; uses `HarmonyPatchCompat` only |
| REPOConfig UI hacks | Could be `Systems/Config/` or `Compat/REPOConfig/` | Large `RepoConfigSliderLabelCompat` is the outlier; keep soft-dep boundary clear |
| Psychotic break input lock | `PsychoticBreakPlayerLockdown` | Duplicates Traverse patterns; could call shared helper later |

**Do not move** patch postfixes into Core: ADR-0009 lifecycle stays in `Plugin.Awake` + patch classes.

**Documentation gap:** `docs/agents/domain.md` runtime map (ARCH-1) lists `Systems/Patches`, `PsychoticBreak/`, etc., but not `Systems/Core/`. ADR-0016 and `reflection-inventory.md` are canonical; update ARCH-1 table when editing agent docs.

**Boot wiring (correct):**

- `Plugin.Start()` → `RepoConfigCompat.TryApply` (line 89)
- `DreadSystemInitializer.TryInitialize()` → same after systems spawn (line 39)
- `_applied` in `RepoConfigSliderLabelCompat` makes double-call safe

## Issues

### [SEVERITY: CRITICAL] CI analyze globs omit nested `Systems/` paths

- **Location:** `.github/workflows/ci.yml` (analyze job, lines 170-198)
- **Category:** maintainability
- **Description:** Grep targets are `*.cs`, `Systems/*.cs`, and `Config/*.cs` only. Files under `Systems/Core/`, `Systems/Patches/`, `Systems/PsychoticBreak/`, `Systems/ErrorReporting/`, and `Systems/DebugOverlay/` are **not scanned** for null-forgiving abuse, 120-char lines, tabs, trailing whitespace, or BOM. Core is structurally important but invisible to the same rules documented in AGENTS.md and `code-quality-reviewer-prompt.md`.
- **Suggested fix:**
  ```bash
  # In ci.yml analyze step, replace flat globs with recursive:
  find . -path './.git' -prune -o \( -name '*.cs' -path './Systems/*' -o -name '*.cs' -maxdepth 1 \) -print
  # Or simpler:
  shopt -s globstar
  FILES=( *.cs Config/*.cs Systems/**/*.cs )
  ```
  Mirror the same paths in `scripts/verify-dread.ps1` if Tier-0 adds style checks later.

### [SEVERITY: IMPORTANT] `PlayerTumbleCompat._forcedActive` is global, not per-player

- **Location:** `PlayerTumbleCompat.cs:13`, `74`, `80-92`
- **Category:** bug
- **Description:** Forced tumble state is a single static flag. Psychotic break only drives the local `PlayerController`, so this works today. Any future second consumer (second local flow, tests, or host+client edge cases) could leave tumble forced on the wrong avatar or block `ReleaseForcedTumble` for another flow. `ReleaseForcedTumble` clears the flag before resolving `pc` (line 92-95), so a null `pc` still resets global state but may not call `TumbleSet(false)` on the avatar.
- **Suggested fix:** Key forced state by `PlayerController` instance ID or `GetInstanceID()` in a `Dictionary<int, bool>`, or store state on a small marker component parented to the player during the episode.

### [SEVERITY: IMPORTANT] `HarmonyPatchCompat.IsMasterClient()` fails open

- **Location:** `HarmonyPatchCompat.cs:20-32`
- **Category:** design
- **Description:** If `SemiFunc.IsMasterClient` is missing or `Invoke` throws, the method returns `true`. Per ADR-0004, host-only monster patches should not run on clients. Fail-open can apply investigate-radius scaling on non-host clients when reflection breaks (stub mismatch, game update), increasing desync risk with Photon-synced `EnemyDirector` state.
- **Suggested fix:** Prefer fail-closed for patch gates: return `false` when method is null or invoke fails, and log once at Warning. Keep fail-open only behind an explicit config if needed for single-player modded environments.

### [SEVERITY: IMPORTANT] Enemy health compat fails open when HP cannot be read

- **Location:** `EnemyHealthCompat.cs:44-47`, `55-58`, `94-95`
- **Category:** design
- **Description:** `IsAliveForVisibility`, `TryIsAlive`, and `CountAliveAndNearby` treat unread HP as alive. That avoids false negatives for LOS/psychotic break but inflates `EnemiesAlive` in error telemetry and debug MCP when field names change. Intentional per comments on visibility, but inconsistent with corpse handling when HP is readable.
- **Suggested fix:** Split APIs: `TryGetHealth` + `IsAlive` (strict) vs `TreatUnknownAsAlive` (documented fail-open). Error reporting should prefer strict counts with a `health_unknown` counter in payload.

### [SEVERITY: IMPORTANT] Dead code: `PlayerControllerCompat.GetStamina`

- **Location:** `PlayerControllerCompat.cs:50-71`
- **Category:** dead-code
- **Description:** No callers in the repository (`rg GetStamina` only hits the definition). `ErrorReportPayloadCapture` uses `player.stamina` directly (line 135), bypassing compat.
- **Suggested fix:** Remove `GetStamina` or wire error reporting through it and delete direct `player.stamina` access in `ErrorReportPayloadCapture.cs`.

### [SEVERITY: IMPORTANT] Error reporting bypasses `PlayerControllerCompat` for player stats

- **Location:** `Systems/ErrorReporting/ErrorReportPayloadCapture.cs:133-135` (caller); Core layer unused
- **Category:** maintainability
- **Description:** Enemy counts use `EnemyHealthCompat` (good), but HP/stamina use compile-time `player.Health` / `player.stamina`. Stubs and game builds may diverge; Core exists partly to centralize that (ADR-0016, `core-enemy-health` contract).
- **Suggested fix:**
  ```csharp
  state.PlayerHp = (int)(PlayerControllerCompat.GetHealth(player) * 100f);
  // After GetStamina is kept or reimplemented:
  state.PlayerStamina = (int)(PlayerControllerCompat.GetStamina(player) * 100f);
  ```

### [SEVERITY: IMPORTANT] `RepoConfigSliderLabelCompat` patches all MenuLib sliders globally

- **Location:** `RepoConfigSliderLabelCompat.cs:39-54`, `88-105`
- **Category:** design
- **Description:** Harmony postfixes attach to every `MenuAPI.CreateREPOSlider` overload in MenuLib, not only Dread's REPOConfig entries. Any mod using empty descriptions gets Dread's layout hacks (label X=100, compact row). Documented as temporary in `domain.md`, but cross-mod blast radius is high.
- **Suggested fix:** Long term: upstream REPOConfig/MenuLib fix (domain.md). Short term: guard postfix with owner/mod id if REPOConfig exposes context, or patch only methods invoked from Dread's config registration path.

### [SEVERITY: IMPORTANT] Widespread empty `catch` blocks in compat probes

- **Location:** All Core files (e.g. `EnemyHealthCompat.cs:145-152`, `PlayerControllerCompat.cs:27-39`, `PlayerTumbleCompat.cs:42-49`, `RepoConfigSliderLabelCompat.cs:158-160`)
- **Category:** maintainability
- **Description:** Matches reflection-inventory **keep/reduce** policy but violates review criterion "no silent failures." Failures are indistinguishable from "member not found," slowing game-update debugging.
- **Suggested fix:** Use `LoggingService.LogVerbose` behind a static `CompatDiagnostics` flag (config-gated), or count failures once per member name per type.

### [SEVERITY: NICE_TO_HAVE] `EnemyHealthCompat.TryReadHealth` recreates `Traverse` per member name

- **Location:** `EnemyHealthCompat.cs:119-132`
- **Category:** performance
- **Description:** Up to 12 `Traverse.Create` calls per enemy per read. Called from scan caches, psychotic break visibility, error batching (not every frame for all paths, but worth tightening).
- **Suggested fix:**
  ```csharp
  var traverse = Traverse.Create(enemy);
  foreach (var name in HealthMemberNames) { ... }
  ```

### [SEVERITY: NICE_TO_HAVE] Duplicated reflection patterns between player compat classes

- **Location:** `PlayerControllerCompat.cs` (`GetCrouchBoolFields`, `TryGetBoolMember`) vs `PlayerTumbleCompat.cs` (`GetTumbleBoolFields`, member name loops)
- **Category:** maintainability
- **Description:** Same "scan bool fields by substring" and Traverse-first patterns. ARCH-2 inventory marks both **reduce** with caches (partially done).
- **Suggested fix:** Extract `CompatReflection.TryReadBoolMembers(object target, string[] names, string fieldSubstring)` in Core (single file) used by both.

### [SEVERITY: NICE_TO_HAVE] `PlayerControllerCompat` visibility inconsistency

- **Location:** `PlayerControllerCompat.cs:18-94` (`public static` on `internal` class)
- **Category:** naming
- **Description:** Class is `internal` but methods are `public`. No external assembly consumers; `public` adds noise.
- **Suggested fix:** Change surface to `internal static` to match other Core types.

### [SEVERITY: NICE_TO_HAVE] `HarmonyPatchCompat.ShouldSkipDueToForeignPatches` ignores Harmony patch kinds beyond four lists

- **Location:** `HarmonyPatchCompat.cs:57-84`
- **Category:** design
- **Description:** Only Prefixes, Postfixes, Transpilers, Finalizers checked. Unlikely issue today; document assumption or use Harmony's full patch enumeration if API allows.
- **Suggested fix:** Comment that IL manipulators are out of scope, or extend if Harmony 2.x exposes them on `Patches`.

### [SEVERITY: NICE_TO_HAVE] `UnityWebRequestCompat` probe uses real network stack

- **Location:** `UnityWebRequestCompat.cs:30-31`
- **Category:** performance
- **Description:** `new UnityWebRequest("http://127.0.0.1/", "HEAD")` on first audio load path may touch networking on some builds. Acceptable for probe; note in comment.
- **Suggested fix:** If issues arise, probe via `typeof(UnityWebRequest).GetMethod` only without constructing a request.

### [SEVERITY: STYLE] Null-forgiving on `FieldInfo.GetValue` results

- **Location:** `PlayerControllerCompat.cs:108`, `PlayerTumbleCompat.cs:56`
- **Category:** style
- **Description:** `(bool)field.GetValue(target)!` is idiomatic but would fail CI if Core were in `Systems/*.cs` glob (`GetValue` returns `object?`).
- **Suggested fix:** `field.GetValue(target) is bool b && b`

### [SEVERITY: STYLE] `RepoConfigCompat` is a one-line pass-through

- **Location:** `RepoConfigCompat.cs:8-14`
- **Category:** structure
- **Description:** Only forwards to `RepoConfigSliderLabelCompat.TryApply`. Useful as stable entry for `Plugin` / initializer; optional inline if file count matters.
- **Suggested fix:** Keep for ADR-0016 table stability, or document as intentional facade.

## Positive Patterns

- **Clear namespace and ADR ownership:** `Dread.Systems.Core` is documented in ADR-0016 with a type table; `reflection-inventory.md` tracks each site with disposition (**keep** / **reduce** / **replace**).
- **Idempotent REPOConfig apply:** `_applied` plus retry from `DreadSystemInitializer` when MenuLib loads late (ADR-0016 boot order).
- **Foreign patch skip:** `HarmonyPatchCompat.ShouldSkipDueToForeignPatches` with one-shot warning respects `CompatibilitySkipConflictingPatches` and other mods (ADR-0009 matrix).
- **Enemy destroyed-object safety:** `EnemyHealthCompat.IsValid` try/catch on `gameObject` avoids Unity fake-null exceptions; `EnemyScanCache` filters before caching.
- **Stub-build audio path:** `UnityWebRequestCompat` gates UWR when stubs have zero RVA; NVorbis primary path in `AudioClipLoader` (ADR-0006/0007).
- **Per-type reflection caches:** `PlayerTumbleCompat` and `PlayerControllerCompat` cache `FieldInfo` / `MethodInfo` by `Type` under locks (ARCH-2 **reduce**).
- **Separation from patches:** Monster patches use compat for gates only; IL stays in `Systems/Patches/`.

## Recommended Agent Prompts

**P0 (CI):**  
> Expand `.github/workflows/ci.yml` analyze step to include `Systems/**/*.cs` (and any other nested source dirs). Re-run analyze locally with the same find/grep commands. Confirm Core files pass 120-char and whitespace rules.

**P1 (tumble state):**  
> Refactor `PlayerTumbleCompat` to track forced tumble per `PlayerController` instance, not a static bool. Update `PsychoticBreakPlayerLockdown` call sites; ensure `RestorePlayerControl` and `PsychoticBreakSystem.OnDestroy` always release the correct player.

**P1 (master client fail-closed):**  
> Change `HarmonyPatchCompat.IsMasterClient()` to return false when `SemiFunc.IsMasterClient` is missing or throws; log one Warning. Run stub build and verify monster patches still apply on host in single-player.

**P2 (error reporting compat):**  
> In `ErrorReportPayloadCapture.CaptureGameState`, use `PlayerControllerCompat.GetHealth` / `GetStamina` (or remove dead `GetStamina` and add stamina names to compat). Add a test or manual note in `CHANGELOG.md` Unreleased if behavior changes.

**P2 (dead code):**  
> Remove `PlayerControllerCompat.GetStamina` if error reporting is wired elsewhere; otherwise implement callers and delete direct `player.stamina` usage.

**P3 (diagnostics):**  
> Add optional verbose compat logging (one line per type/member failure) behind `DreadConfig` debug flag; no hot-path spam.

## Cross-references

| Doc | Relevance |
|-----|-----------|
| [ADR-0016](../adr/0016-arch-3-extension-model.md) | Core folder purpose, boot order, compat matrix |
| [ADR-0004](../adr/0004-host-authoritative-monster-changes.md) | `IsMasterClient` host gates |
| [ADR-0009](../adr/0009-toggleable-harmony-patches.md) | Patch lifecycle vs Core helpers |
| [ADR-0011](../adr/0011-psychotic-break-system.md) | `PlayerTumbleCompat` / hide vulnerability |
| [ADR-0006](../adr/0006-unitywebrequest-for-audio-loading.md) | UWR loading; `UnityWebRequestCompat` for stubs |
| [domain.md](../agents/domain.md) | REPOConfig slider compat (temporary); ARCH-1 map needs Core row |
| [reflection-inventory.md](../agents/guides/reflection-inventory.md) | Per-file disposition for all Core types |
| [docs/repo-config-slider-labels-investigation.md](../repo-config-slider-labels-investigation.md) | REPOConfig UI mitigation details |
| [specs/004-err-2-default-on-prompt/contracts/core-enemy-health.md](../../specs/004-err-2-default-on-prompt/contracts/core-enemy-health.md) | Enemy health compat contract |

### Caller map (grep, 2026-06-01)

| Core API | Callers |
|----------|---------|
| `EnemyHealthCompat.*` | `EnemyScanCache`, `PsychoticBreakTrigger`, `DebugServerSystem`, `ErrorReportPayloadCapture` |
| `HarmonyPatchCompat.*` | `EnemyNavMeshAgentAwakePatch`, `EnemyDirectorSetInvestigatePatch` |
| `PlayerControllerCompat.*` | `PsychoticBreakTrigger` (`IsAlive`, `IsHidingVulnerable`) |
| `PlayerTumbleCompat.*` | `PsychoticBreakPlayerLockdown`, `PlayerControllerCompat.IsHidingVulnerable` |
| `RepoConfigCompat.TryApply` | `Plugin.cs:89`, `DreadSystemInitializer.cs:39` |
| `UnityWebRequestCompat.IsUsable` | `AudioClipLoader.cs:49` |
