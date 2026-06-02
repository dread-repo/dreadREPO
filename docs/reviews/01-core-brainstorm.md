# Feature brainstorm: Core (compat layer)

**Based on:** [01-core-review.md](01-core-review.md)  
**Scope note:** **Full brainstorm.** The Core review focused on compat correctness, fail-open semantics, and CI gaps, not folder governance. Product opportunities sit in multiplayer safety, diagnostics, and cross-mod coexistence.

## Context

`Systems/Core/` is Dread's version-tolerance layer: reflection helpers, Harmony gates, REPOConfig UI compat, and UWR probing (ADR-0016). Players never "see" Core directly, but Core quality determines whether monster patches, psychotic break triggers, error payloads, and audio loading behave correctly across game updates and modded lobbies. Improvements here unlock safer host authority, richer debug/MCP state, and fewer silent failures when REPO field names change.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Compat health snapshot | S | Low | Maintainers + MCP verify |
| 2 | Fail-closed host gates (product) | S | Med | Co-op integrity |
| 3 | Strict vs permissive enemy HP for telemetry | M | Low | Better error reports |
| 4 | Compat diagnostics overlay rows | S | Low | Faster game-update triage |
| 5 | Mod coexistence matrix in MCP | M | Med | Support / compat testing |
| 6 | Optional stamina/health compat for all consumers | S | Low | One source of truth |
| 7 | REPOConfig scope guard (Dread-only sliders) | M | High | Cross-mod etiquette |
| 8 | Version probe + "game build mismatch" toast | M | Med | Player clarity after updates |

### Detail

#### Compat health snapshot

- **Value:** Exposes which compat probes succeeded (master client, enemy HP field, tumble fields, UWR) in `DreadRuntimeState` and MCP `get_runtime_state`. Agents and hosts diagnose stub/game mismatches without reading BepInEx logs.
- **Effort:** S  
- **Dependencies:** `DreadRuntimeState`, `DebugServerSystem`, `HarmonyPatchCompat`, `EnemyHealthCompat`  
- **Risk:** Low  
- **Skip if:** DBG-5 overlay API lands first and can host the same rows without Core changes.

#### Fail-closed host gates (product behavior)

- **Value:** When `SemiFunc.IsMasterClient` cannot be resolved, monster Harmony patches do not run on clients. Prevents investigate-radius and NavMesh tweaks from desyncing co-op runs after a game patch.
- **Effort:** S (behavior change + one-time Warning log)  
- **Dependencies:** `HarmonyPatchCompat`, monster patches (ADR-0004)  
- **Risk:** Med (single-player edge cases if fail-closed is too aggressive)  
- **Skip if:** Product accepts fail-open for maximum modded solo compatibility.

#### Strict vs permissive enemy HP for telemetry

- **Value:** Error reports and MCP state include `enemies_alive`, `enemies_unknown_hp`, and `enemies_dead` separately instead of treating unread HP as alive everywhere. Improves triage without breaking psychotic break visibility fail-open.
- **Effort:** M  
- **Dependencies:** `EnemyHealthCompat`, `ErrorReportPayloadCapture`, ADR-0010 payloads  
- **Risk:** Low  
- **Skip if:** ERR-2b core capture PR already ships a richer schema.

#### Compat diagnostics overlay rows

- **Value:** F10 debug overlay section "Compat" shows master-client probe result, last foreign-patch skip, REPOConfig apply state. Reduces time to answer "why didn't aggression apply?"
- **Effort:** S  
- **Dependencies:** DBG-5 (soft), UI-1 (soft), `LoggingService` verbose flag  
- **Risk:** Low  
- **Skip if:** Compat health snapshot is MCP-only and overlay stays minimal.

#### Mod coexistence matrix in MCP

- **Value:** `dread_verify` adds checks: foreign patches on shared methods, REPOConfig loaded, MenuLib present. Returns structured `compat_warnings[]` for support threads.
- **Effort:** M  
- **Dependencies:** `HarmonyPatchCompat.ShouldSkipDueToForeignPatches`, `PluginDependencyResolver`, MCP formatters (08-review fixes)  
- **Risk:** Med (false positives when many mods patch same targets)  
- **Skip if:** Manual compat guide in `mod-compatibility.md` is sufficient.

#### Optional stamina/health compat for all consumers

- **Value:** Wire `PlayerControllerCompat` through tension, error capture, and debug server so one field-name list survives game updates.
- **Effort:** S  
- **Dependencies:** ERR-2b / Core review follow-up  
- **Risk:** Low  
- **Skip if:** Game never renames those fields and direct compile-time access stays stable.

#### REPOConfig scope guard (Dread-only sliders)

- **Value:** MenuLib postfixes only adjust sliders registered from Dread's config path, reducing layout side effects on other mods' REPOConfig screens.
- **Effort:** M  
- **Dependencies:** `RepoConfigSliderLabelCompat`, upstream REPOConfig context (may be unavailable)  
- **Risk:** High (reflection/context may not exist)  
- **Skip if:** DBG-4 upstream fix removes compat entirely.

#### Version probe + "game build mismatch" toast

- **Value:** On boot, detect known game assembly version/hash; if unknown, show one non-blocking in-game notice (via future NOTIF-1) linking to Thunderstore compatibility notes.
- **Effort:** M  
- **Dependencies:** NOTIF-1, UI-1, optional version file in package  
- **Risk:** Med (false alarms on benign updates)  
- **Skip if:** Dread only targets pinned REPO versions with strict manifest deps.

## Quick wins vs strategic bets

| Quick wins (ship in days) | Strategic bets (multi-PR) |
|---------------------------|---------------------------|
| Fail-closed master client | Mod coexistence matrix + verify integration |
| Wire `PlayerControllerCompat` to error capture | REPOConfig scope guard |
| Compat rows on `DreadRuntimeState` | Version probe + player-facing mismatch notice |
| Strict/permissive HP split for telemetry | Full compat diagnostics behind debug cfg |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| CORE-1 | **Compat health in runtime state + MCP** | P2 | ARCH-3 (done) |
| CORE-2 | **Fail-closed `IsMasterClient` for monster patches** | P1 | None |
| CORE-3 | **Telemetry HP strict/unknown counters in payloads** | P2 | ERR-2b (soft) |
| CORE-4 | **REPOConfig slider postfix scoped to Dread entries** | P3 | DBG-4 upstream (soft) |
