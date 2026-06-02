# Feature brainstorm: UI (presentation layer)

**Based on:** [02-ui-review.md](02-ui-review.md)  
**Scope note:** **Features only.** The UI review proposed `Systems/UI/` layout, file moves, and governance. This addendum covers player/developer UX capabilities, not additional folder proposals.

## Context

Dread's on-screen surfaces today are fragmented: F10 IMGUI debug HUD, ERR-2 consent modal, and psychotic break uGUI vignette, with duplicated theme math and no shared notification channel. ROADMAP **UI-1** and **DBG-1/3/5** already target kit + overlay polish. The brainstorm below extends those with horror-appropriate player affordances and agent-friendly debug UX that reuse `DreadRuntimeState`, tension/break telemetry, and MCP.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Unified Dread theme (UI-1 extension) | L | Low | All IMGUI surfaces |
| 2 | Overlay section registry (DBG-5) | M | Low | Feature authors |
| 3 | In-run tension meter (minimal HUD) | M | Med | Players |
| 4 | Episode-safe UI stacking policy | S | Low | Prompt + break + overlay |
| 5 | Accessibility: scale + high-contrast theme | M | Low | Players (Proton/Linux) |
| 6 | "Hold to confirm" for destructive debug actions | S | Med | Accidental MCP clicks |
| 7 | Psychotic break accessibility toggle | S | Low | Photosensitivity |
| 8 | Config preview panel in overlay | M | Low | Hosts tuning dread |

### Detail

#### Unified Dread theme (extends UI-1)

- **Value:** One `DreadImGuiTheme` supplies accent/dim/body colors, button states, and Proton-safe textures for consent modal, F10 overlay, and future toasts. Stops visual drift between ERR-2 and debug HUD.
- **Effort:** L (already ROADMAP UI-1)  
- **Dependencies:** UI-1, `OverlayTextureUtil` / shared `ImGuiTexture`  
- **Risk:** Low  
- **Skip if:** UI-1 ships with only two consumers and no extension hooks.

#### Overlay section registry (DBG-5)

- **Value:** Features register read-only rows and toggles (`RegisterSection`, `IOverlayRow`) without editing `DebugOverlayPanel`. Tension, psychotic break, and error reporter expose live block reasons and queue depth.
- **Effort:** M  
- **Dependencies:** DBG-5, UI-1 primitives, ARCH-3 registry  
- **Risk:** Low  
- **Skip if:** Overlay remains maintainer-only with hardcoded rows.

#### In-run tension meter (minimal HUD)

- **Value:** Optional cfg-gated corner bar showing nearest-enemy band (safe / uneasy / panic) driven by existing proximity scan. Gives players feedback without numbers-heavy HUD.
- **Effort:** M  
- **Dependencies:** `TensionSystem`, `DreadRuntimeState`, UI-1, NOTIF-1 or lightweight uGUI strip  
- **Risk:** Med (clutter vs horror minimalism)  
- **Skip if:** Design keeps tension audio-only.

#### Episode-safe UI stacking policy

- **Value:** Central `UiStackPolicy`: when ERR-2 modal or psychotic break episode is active, suppress F10 overlay and route z-order (`GUI.depth`) from one place. Prevents input-lock conflicts documented in the review.
- **Effort:** S  
- **Dependencies:** `ErrorReportingPromptSystem`, `PsychoticBreakSystem`, `DebugOverlaySystem`  
- **Risk:** Low  
- **Skip if:** Episodes and prompt are mutually exclusive by scene gates today.

#### Accessibility: scale + high-contrast theme

- **Value:** CFG multipliers for IMGUI font size and a high-contrast palette (addresses DBG-3 font pain on Proton/Linux as player-facing option, not only debug fonts).
- **Effort:** M  
- **Dependencies:** UI-1, DBG-3 (soft)  
- **Risk:** Low  
- **Skip if:** REPOConfig or game UI scale covers all needs.

#### "Hold to confirm" for destructive debug actions

- **Value:** Overlay buttons for test crash or force break require 1s hold, mirroring MCP `destructiveHint`. Reduces accidental triggers during livestreams or verify sessions.
- **Effort:** S  
- **Dependencies:** Debug overlay, `TestCrashSystem`, psychotic break debug force  
- **Risk:** Med (friction for agents)  
- **Skip if:** Destructive actions remain MCP-only.

#### Psychotic break accessibility toggle

- **Value:** Config to reduce vignette alpha max, disable peak scream, or shorten episode for photosensitive players while keeping tension/audio dread.
- **Effort:** S  
- **Dependencies:** `PsychoticBreakOverlay`, `DreadConfig`, ADR-0011 tuning  
- **Risk:** Low  
- **Skip if:** `CompatibilityMode` already covers the audience.

#### Config preview panel in overlay

- **Value:** Read-only grouped cfg browser in F10 (uses same sections as MCP `get_config`) so hosts tune dread in-run without alt-tabbing to BepInEx file.
- **Effort:** M  
- **Dependencies:** DBG-5, `DebugServerSystem` config mirror, UI-1 scroll body  
- **Risk:** Low  
- **Skip if:** MCP + REPOConfig in-game menu is enough.

## Quick wins vs strategic bets

| Quick wins | Strategic bets |
|------------|----------------|
| UI stacking policy + texture cleanup (review P2) | UI-1 full kit |
| Episode accessibility toggles | In-run tension meter |
| Hold-to-confirm on destructive overlay actions | DBG-5 extensible registry + config preview |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| UI-2 | **Optional in-run tension proximity meter** | P2 | UI-1, TensionSystem |
| UI-3 | **Central UI stack / z-order policy for modals vs overlay** | P2 | UI-1 (soft) |
| UI-4 | **Psychotic break accessibility preset (reduced FX)** | P2 | None |
| UI-5 | **High-contrast + font scale theme variants** | P2 | UI-1, DBG-3 (soft) |

*Note: UI-1, DBG-1, DBG-3, DBG-5 remain canonical; above IDs extend without replacing them.*
