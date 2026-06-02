# Feature brainstorm: Notifications (player messaging)

**Based on:** [03-notifications-review.md](03-notifications-review.md)  
**Scope note:** **Features only.** The notifications review defined hybrid `Systems/UI/Notifications/` placement and migration phases. Here: product capabilities for non-blocking player messages, not folder moves or registry group renames.

## Context

Dread has no toast/notification channel today. Players get blocking ERR-2 consent once, ambient audio/tension feedback, and rare psychotic break FX. Operators get BepInEx logs. A small **NotificationHost** (per review design) would let features signal state changes (telemetry failures, tension milestones, co-op warnings) without coupling to `ErrorReporting/` or the debug HUD.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Toast queue host (NOTIF-1) | M | Low | Foundation |
| 2 | Error upload failure toast | S | Low | Players who opted in |
| 3 | Co-op host-only patch skipped notice | S | Med | Lobby clarity |
| 4 | Psychotic break "something feels wrong" tease | M | Med | Horror pacing |
| 5 | Tension tier whisper toasts | M | Med | Players |
| 6 | First-run "Dread active" subtle banner | S | Low | Mod discovery |
| 7 | MCP-triggered dev toast (debug only) | S | Low | Agent verify |
| 8 | Queue priority + severity colors | M | Low | UX polish |

### Detail

#### Toast queue host (NOTIF-1)

- **Value:** `NotificationHost.Enqueue(message, severity, duration)` with menu-level suppression, max 2 visible, no input lock. Shared theme from UI-1.
- **Effort:** M  
- **Dependencies:** UI-1 (soft), `SystemOrderGroup.Ui` (implementation detail)  
- **Risk:** Low  
- **Skip if:** All player messaging stays audio-only forever.

#### Error upload failure toast

- **Value:** After repeated batch POST failures (ERR-4), show one toast: "Crash reports could not be sent" with opt-out hint. Does not replace privacy modal.
- **Effort:** S  
- **Dependencies:** NOTIF-1, `ErrorReporterSystem`, ERR-4  
- **Risk:** Low  
- **Skip if:** Console-only diagnosis is preferred for privacy.

#### Co-op host-only patch skipped notice

- **Value:** When client detects monster patches skipped due to fail-closed master probe, one toast per session: "Monster tweaks apply on host only." Reduces "Dread broken on client" reports.
- **Effort:** S  
- **Dependencies:** NOTIF-1, CORE-2 fail-closed behavior, multiplayer  
- **Risk:** Med (noise in listen-server edge cases)  
- **Skip if:** Silent fail-closed is intentional.

#### Psychotic break "something feels wrong" tease

- **Value:** Optional subtle toast 2s before episode when `CanTrigger` is true but roll not yet fired (cfg off by default). Increases dread without spoiling mechanics.
- **Effort:** M  
- **Dependencies:** `PsychoticBreakTrigger`, NOTIF-1  
- **Risk:** Med (may reduce surprise)  
- **Skip if:** Design keeps break fully unannounced.

#### Tension tier whisper toasts

- **Value:** Rare text cues at band crossings ("You're being hunted") tied to proximity scan, rate-limited once per minute.
- **Effort:** M  
- **Dependencies:** `TensionSystem`, NOTIF-1  
- **Risk:** Med (breaks immersion if overused)  
- **Skip if:** UI-2 tension meter ships instead.

#### First-run "Dread active" subtle banner

- **Value:** Non-blocking 5s banner on first gameplay level when mod loads (separate from ERR-2 consent). Helps players know atmosphere systems are on.
- **Effort:** S  
- **Dependencies:** NOTIF-1, cfg `ShowWelcomeBanner`  
- **Risk:** Low  
- **Skip if:** Thunderstore description is enough.

#### MCP-triggered dev toast (debug only)

- **Value:** `dread_show_toast` MCP command for Tier 1 verify of notification pipeline without gameplay side effects.
- **Effort:** S  
- **Dependencies:** NOTIF-1, `DebugServerSystem`, MCP tool  
- **Risk:** Low  
- **Skip if:** Verify uses IMGUI overlay only.

#### Queue priority + severity colors

- **Value:** Errors preempt info toasts; duplicate messages collapse within 10s. Uses UI-1 severity palette.
- **Effort:** M  
- **Dependencies:** NOTIF-1, UI-1  
- **Risk:** Low  
- **Skip if:** Queue stays single-message only.

## Quick wins vs strategic bets

| Quick wins | Strategic bets |
|------------|----------------|
| NOTIF-1 scaffold + MCP test toast | Tension/break tease toasts |
| Upload failure toast (config-gated) | Priority queue + severity system |
| Welcome banner | Co-op host patch notice |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| NOTIF-1 | **Non-blocking toast host + `Enqueue` API** | P2 | UI-1 (soft) |
| NOTIF-2 | **Error reporting upload failure toast (opt-in)** | P2 | NOTIF-1, ERR-4 (soft) |
| NOTIF-3 | **Session welcome banner (first level)** | P3 | NOTIF-1 |
| NOTIF-4 | **Tension tier optional text cues** | P3 | NOTIF-1, TensionSystem |
| NOTIF-5 | **MCP `dread_show_toast` for verify** | P3 | NOTIF-1, MCP |
