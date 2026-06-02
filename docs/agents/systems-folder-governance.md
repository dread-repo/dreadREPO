# Systems folder governance (agents)

Rules for placing and extending code under `Systems/`. Complements [domain.md](domain.md) ARCH-1 map, [ADR-0016](../adr/0016-arch-3-extension-model.md), and [specs/002-arch-3-extensible-core/contracts/system-lifecycle.md](../../specs/002-arch-3-extensible-core/contracts/system-lifecycle.md).

## Golden rules

1. **No new loose files in `Systems/` root.** Every new `.cs` file goes in a feature or infrastructure subfolder (see placement table below).
2. **Runtime `MonoBehaviour` systems register in `DreadSystemRegistry` only.** Never add `TryAddSystem` or `AddComponent` calls in `Plugin.cs` (ADR-0016, `scripts/verify-dread.ps1` `arch3_try_add_system`).
3. **Harmony patches stay in `Systems/Patches/`.** Patch apply/remove stays in `Plugin.Awake` (ADR-0009). Patches may call `Systems/Core` compat helpers only.
4. **Game-version tolerance stays in `Systems/Core/`.** Do not add reflection for `EnemyHealth`, `PlayerController`, Harmony gates, REPOConfig, or UWR outside Core (ADR-0016).
5. **Namespace stays `Dread.Systems` (and `Dread.Systems.Core` for Core).** Subfolders are organizational; do not introduce per-folder namespaces unless an ADR says otherwise.
6. **Finish ARCH-1 before large feature work in flat paths.** If you touch a root-level file, prefer moving it to the target folder in the same PR (or file a follow-up issue with the folder map from [07-systems-loose-files-review.md](../reviews/07-systems-loose-files-review.md)).

## Where to put new code

| You are adding… | Folder | Examples |
|-----------------|--------|----------|
| New gameplay/runtime system (`MonoBehaviour`) | Feature folder + registry row | `Systems/Tension/`, `Systems/Audio/` |
| Harmony patch | `Systems/Patches/` | `*Patch.cs` |
| Stub/game compat helper | `Systems/Core/` | `*Compat.cs` |
| Error telemetry (queue, upload, consent) | `Systems/ErrorReporting/` | Existing partials |
| Player IMGUI / shared textures (UI-1) | `Systems/UI/` (when created) | Overlay, prompt, `OverlayTextureUtil` |
| TCP/MCP debug API | `Systems/Debug/` | `DebugServerSystem`, `TestCrashSystem` |
| Boot/registry/init | `Systems/Bootstrap/` | Registry, initializer, dependency resolver |
| Cross-system debug snapshot | `Systems/Runtime/` | `DreadRuntimeState` |
| Shared enemy scan (0.5s cache) | `Systems/Scan/` or feature that owns it | `EnemyScanCache` |
| Logging wrapper | `Systems/Infrastructure/` or keep `LoggingService` in Infrastructure | BepInEx log facade |
| JSON/DTO for error worker | `Systems/ErrorReporting/` (same feature as reporter) | `ErrorReportJson`, `ErrorReportTypes` |
| One-off marker `MonoBehaviour` on game objects | Same feature as caller | `DreadAudioTweaked` with monster audio |

## `DreadSystemRegistry` checklist

When adding a registry row:

- [ ] Unique `id` string (kebab-case).
- [ ] `SystemOrderGroup.Core` for gameplay/telemetry; `Debug` for overlay, debug server, test crash.
- [ ] `HostName` unique (`Dread*Host`).
- [ ] Optional `IsEnabled: () => DreadConfig.*` if the system should not spawn when off (prefer self-disable in `Start` only when host must exist for MCP).
- [ ] Document new fields on `DreadRuntimeState` in ADR-0016 table if overlay/MCP need them.
- [ ] Update `scripts/verify-dread.ps1` manifest if baseline system count changes.

## Shared caches and scans

- Prefer **`EnemyScanCache`** for `EnemyHealth` enumeration on a ~0.5s cadence. Call `Invalidate()` when enemy population may change (e.g. level load).
- Do **not** add another `FindObjectsOfType<EnemyHealth>()` loop in `Update` without justification.
- If you need nearest-enemy distance, use `EnemyScanCache.NearestDistance` (or extend the cache) instead of a private duplicate scan.

## File size and splits

| Threshold | Action |
|-----------|--------|
| > ~400 lines, multiple concerns | Split partials (see `PsychoticBreak/`, `DebugOverlay/`) or extract helpers |
| > ~800 lines, distinct protocols | Split by concern (e.g. `DebugServer` command dispatch vs TCP listener) |
| Static util used by 2+ features | `Systems/UI/Shared/`, `Systems/Core/`, or `Systems/Scan/` |

## Post-implementation refactor (required habit)

After a feature ships:

1. Move new files out of `Systems/` root if any were added there.
2. Remove dead APIs (grep callers repo-wide).
3. Wire gameplay code through Core compat where ADR-0016 applies.
4. Update `docs/agents/domain.md` ARCH-1 table and `CONTEXT.md` file map if boundaries changed.
5. Add a line under `[Unreleased]` in `CHANGELOG.md` only for player-visible or operator-visible changes.

## CI note

`.github/workflows/ci.yml` analyze step greps `Systems/*.cs` (root only), not `Systems/**/*.cs`. Root loose files are style-checked; nested folders are not. When expanding globs, use `Systems/**/*.cs` for parity with AGENTS.md intent.

## Related docs

- [07-systems-loose-files-review.md](../reviews/07-systems-loose-files-review.md): current loose inventory, dead code, proposed moves
- [02-ui-review.md](../reviews/02-ui-review.md): `Systems/UI/` target layout
- [06-error-reporting-review.md](../reviews/06-error-reporting-review.md): error JSON placement
