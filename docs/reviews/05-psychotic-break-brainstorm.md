# Feature brainstorm: Psychotic Break

**Based on:** [05-psychotic-break-review.md](05-psychotic-break-review.md)  
**Scope note:** **Full brainstorm.** The psychotic break review focused on lifecycle bugs, ADR drift, and performance. The feature is already a flagship horror beat; below are extensions that deepen co-op dread and tooling without restructuring partials.

## Context

Psychotic Break is a client-local ~20s episode: solo + recent threat + LoS lost + hiding vulnerability, with vignette, phased audio, input lockdown, and stumble exit. It integrates with `EnemyScanCache`, tension's threat vocabulary, and debug/MCP force. Ideas below build on that state machine while respecting `OncePerMatch` and `CompatibilityMode`.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Episode variants (Whisper / Hunt / Void) | L | Med | Replayability |
| 2 | Co-op "echo" hint (other players) | M | High | Social horror |
| 3 | Post-episode adrenaline crash | S | Low | Tension synergy |
| 4 | Threat memory decay UI (debug/optional) | S | Low | Tuning |
| 5 | Break-adjacent fake radio static | M | Low | Atmosphere |
| 6 | MCP episode timeline export | S | Low | Agents |
| 7 | "Near miss" counter (almost triggered) | M | Low | Analytics/opt-in |
| 8 | Clip-gated debug force guard | S | Low | Verify quality |

### Detail

#### Episode variants (Whisper / Hunt / Void)

- **Value:** Cfg selects phase curves: Whisper (audio-heavy, low alpha), Hunt (footsteps only), Void (max darkness, no phantoms). Same trigger rules, different `_episodeProfile`.
- **Effort:** L  
- **Dependencies:** `PsychoticBreakEpisode`, `PsychoticBreakAudio`, `DreadConfig`  
- **Risk:** Med (QA matrix multiplies)  
- **Skip if:** Single canonical episode is brand identity.

#### Co-op "echo" hint (other players)

- **Value:** When local player enters episode, with low chance play distant teammate voice line or footstep on **other** clients only (no network sync of episode). Controversial; cfg off by default.
- **Effort:** M  
- **Dependencies:** Photon/voice research, privacy review  
- **Risk:** High (desync, consent)  
- **Skip if:** Strict client-local contract must never affect others.

#### Post-episode adrenaline crash

- **Value:** For 30s after episode, adrenaline relief reduced and panic sprint cooldown doubled. Links break to `TensionSystem` without new scans.
- **Effort:** S  
- **Dependencies:** `TensionSystem`, `PsychoticBreakSystem` end hook  
- **Risk:** Low  
- **Skip if:** Post-break stumble is enough debuff.

#### Threat memory decay UI (debug/optional)

- **Value:** F10 row or cfg debug shows seconds of `Recent threat` remaining. Helps designers tune `PsychoticBreakTrigger`.
- **Effort:** S  
- **Dependencies:** DBG-5, `DreadRuntimeState` (already publishes block reasons)  
- **Risk:** Low  
- **Skip if:** MCP runtime state is enough.

#### Break-adjacent fake radio static

- **Value:** 5s before eligible roll, rare radio static on tension channel (not a toast). Signals "something building" without announcing break.
- **Effort:** M  
- **Dependencies:** `AudioDreadSystem`, trigger timers  
- **Risk:** Low  
- **Skip if:** NOTIF-4 tease toasts preferred.

#### MCP episode timeline export

- **Value:** `get_runtime_state` adds `psychoticBreakPhase` and `secondsRemaining` during episode; MCP text timeline for verify scripts.
- **Effort:** S  
- **Dependencies:** `PsychoticBreakEpisode`, ADR-0013  
- **Risk:** Low  
- **Skip if:** Overlay rows cover it.

#### "Near miss" counter (almost triggered)

- **Value:** Opt-in telemetry field: count frames where all guards pass except roll. Informs balance without player-visible UI.
- **Effort:** M  
- **Dependencies:** Error reporting payload extension, privacy bullets  
- **Risk:** Low  
- **Skip if:** No analytics appetite.

#### Clip-gated debug force guard

- **Value:** `ForceEpisodeForDebug` requires loaded clips; MCP returns clear error. Review P2 item as product-quality for agents.
- **Effort:** S  
- **Dependencies:** `PsychoticBreakAudio`, MCP  
- **Risk:** Low  
- **Skip if:** Silent debug episodes are acceptable.

## Quick wins vs strategic bets

| Quick wins | Strategic bets |
|------------|----------------|
| Debug force clip guard | Episode variants |
| Post-episode tension debuff | Co-op echo (research) |
| MCP phase fields | Fake radio static lead-in |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| PB-1 | **Psychotic break episode profiles (cfg-selected)** | P2 | AUDIO-1 (done) |
| PB-2 | **Post-episode tension debuff window** | P2 | TensionSystem |
| PB-3 | **Runtime phase timer in MCP/overlay** | P2 | DBG-5 (soft) |
| PB-4 | **Co-op echo audio research spike** | P3 | Design sign-off |
