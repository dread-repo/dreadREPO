# Code Review: Systems/Notifications (and player messaging surfaces)

**Reviewed:** 2026-06-01  
**Scope:** In-game player messaging, toast/notification architecture, `LoggingService`, BepInEx logging, ERR-2 prompt vs notifications, registry/coupling. Builds on [01-core-review.md](01-core-review.md) and [02-ui-review.md](02-ui-review.md).  
**Files inventoried:** `Systems/LoggingService.cs`, `Systems/ErrorReporting/*` (consent, prompt, reporter), `Systems/DebugOverlay/*`, `Systems/DreadSystemRegistry.cs`, `Plugin.cs`, `Config/DreadConfig.cs` (error-reporting keys), ROADMAP/specs/ADR references

## Executive Summary

**There is no `Systems/Notifications/` folder and no in-game notification or toast system** in this repository (confirmed: zero `*Notification*` files, zero `notification`/`toast` matches in `*.cs`). Player-visible surfaces today are three separate channels:

| Channel | Mechanism | Audience | Blocking? |
|---------|-----------|----------|-----------|
| **ERR-2 first-run prompt** | `ErrorReportingPromptSystem` IMGUI modal | Player | Yes (input lock + cursor unlock) |
| **F10 debug overlay** | `DebugOverlay/*` IMGUI HUD | Developer / power user | No |
| **Psychotic break vignette** | `PsychoticBreakOverlay` uGUI canvas | Player (gameplay FX) | No (episode effect, not messaging) |

**`LoggingService`** (ADR-0014) is a **BepInEx console** verbosity gate only. It is not a notification API; ~26 files call it, all writing to `Plugin.Logger`. No `BepInEx` in-game notification APIs, `MessageBox`, or game-native toast hooks appear anywhere.

The UI review finding holds: **error reporting consent is a modal, not a notification.** It correctly uses `ErrorReportingConsent` to block telemetry until `ErrorReportingPromptShown` is true (ADR-0010, ERR-2 contract). That gate must not be reused as a generic “can we show UI?” API for future toasts.

**ROADMAP** plans **UI-1** (shared IMGUI kit for prompt + debug overlay) but does **not** yet track a dedicated notifications/toast backlog item. Future player messages (tension milestones, upload failures, break warnings) will need an explicit `NOTIF-*` or UI-1 extension so agents do not stuff toasts into `ErrorReporting/`.

Main risks: **channel confusion** (console log vs in-game message), **prompt living under ErrorReporting** (invites notification code beside upload logic), **no registry row or `SystemOrderGroup` for UI/notifications**, and **shared CI glob gap** for nested `Systems/**` (same as prior reviews).

## File Structure Assessment

### What exists (2026-06-01 tree)

```
Systems/
  LoggingService.cs              # BepInEx log levels only (NOT notifications)
  ErrorReporting/
    ErrorReportingConsent.cs     # Telemetry gate (cfg flags)
    ErrorReportingPromptSystem.cs # Only player "prompt" UI (IMGUI modal)
    ErrorReporterSystem.cs       # logMessageReceived -> Worker (no OnGUI)
    ErrorReportingPrivacyCopy.cs # Canonical disclosure strings
    ...
  DebugOverlay/                  # F10 HUD (debug, not notifications)
  PsychoticBreak/
    PsychoticBreakOverlay.cs     # uGUI vignette (gameplay, not toast queue)
  (no Notifications/)
```

| Path | Role | Notification bucket? |
|------|------|----------------------|
| `Systems/LoggingService.cs` | Level-gated `Plugin.Logger` | **No**: operator/dev console |
| `Systems/ErrorReporting/ErrorReportingPromptSystem.cs` | One-time consent modal | **Modal**, not toast |
| `Systems/ErrorReporting/ErrorReportingConsent.cs` | `IsReportingAllowed()` | Domain policy, not UI |
| `Systems/ErrorReporting/ErrorReporterSystem.cs` | Telemetry pipeline | Logs outcomes via `LoggingService` only |
| `Systems/DebugOverlay/*` | Live state HUD | Debug surface |
| `Plugin.cs` | `ManualLogSource`, boot | BepInEx only |

### Planned vs missing

| Planned (docs) | Status |
|----------------|--------|
| `Systems/UI/Notifications/` (02-ui-review proposal) | **Not created** |
| `INotificationPresenter` / `EnqueueToast` API | **Not created** |
| ROADMAP **UI-1** unified IMGUI kit | `idea`: covers modal/overlay, not toasts explicitly |
| ROADMAP notification/toast issue | **Missing**: recommend filing `NOTIF-1` after UI-1 |

### Registry (`DreadSystemRegistry.cs`)

| Id | Type | `SystemOrderGroup` | Notes |
|----|------|-------------------|--------|
| `error-reporting-prompt` | `ErrorReportingPromptSystem` | **Core** | Presentation registered as core gameplay |
| `error-reporter` | `ErrorReporterSystem` | Core | No UI |
| `debug-overlay` | `DebugOverlaySystem` | Debug | F10 HUD |

No `notification-host` row. `SystemOrderGroup` has only `Core` and `Debug` (no `Ui`).

### ErrorReportingPrompt: modal semantics (not notification)

Verified behavior in `ErrorReportingPromptSystem.cs`:

- **Activation:** `TryActivatePrompt` on gameplay scenes when `ErrorReportingPromptShown` is false; suppressed on `SemiFunc.MenuLevel()` (matches psychotic break / overlay gates).
- **Blocking:** `Update` locks local player input; `MaintainCursorForPrompt` unlocks cursor; `GUI.depth = 10000` stacks above debug overlay.
- **Dismissal:** Buttons set `ErrorReportingEnabled` and `ErrorReportingPromptShown`; `ErrorReportingConsent` then allows enqueue/send.
- **Not ephemeral:** No queue, duration, or non-blocking corner toast: by design per ERR-2 contract (`specs/004-err-2-default-on-prompt/contracts/first-run-prompt.md`).

### LoggingService vs BepInEx “notifications”

`LoggingService` (ADR-0014):

- Maps `LogLevel` enum to `Plugin.Logger.LogError/Warning/Info/Debug`.
- `LogVerbose` prefixes `[V]` on `LogInfo`.
- `PrintAsciiArt()` is unconditional on load.
- **Does not** touch Unity UI, game HUD, or BepInEx GUI notification plugins.

`ErrorReporterSystem` on `ErrorReportingEnabled` change logs **full privacy copy to the BepInEx console** (`OnErrorReportingSettingChanged`). That is **developer-facing**, not an in-game toast. Players who enable reporting via cfg without reading the prompt still see disclosure only if they open the log.

**BepInEx in-game notifications:** No usage of BepInEx notification/message APIs in `*.cs` (only `BaseUnityPlugin`, `ConfigEntry`, `ManualLogSource`).

### Coupling map (systems that may need messaging later)

| System | Player-visible today | Likely future need | Coupling risk |
|--------|---------------------|-------------------|---------------|
| `TensionSystem` | Audio/state only | Optional tension tier toast | May duplicate IMGUI if no API |
| `PsychoticBreakSystem` | Overlay + audio | Episode start hint? | uGUI vs IMGUI split |
| `ErrorReporterSystem` | None in-game | “Report failed” / offline queue | Must not use prompt host |
| `AudioDreadSystem` | Audio | None typical | Low |
| `MonsterOverhaulSystem` | Gameplay | None typical | Low |
| `TestCrashSystem` | Kill process | None | Debug only |
| `DebugServerSystem` | MCP/TCP | Tool responses only | Out of scope |

Today coupling is **pattern duplication** (IMGUI colors, `MakeTexture`, input lock reflection) documented in 02-ui-review, not a notification module.

## Issues

### [SEVERITY: CRITICAL] CI analyze globs omit nested `Systems/` paths

- **Location:** `.github/workflows/ci.yml` (analyze job); see [01-core-review.md](01-core-review.md), [02-ui-review.md](02-ui-review.md)
- **Category:** maintainability
- **Description:** `ErrorReportingPromptSystem.cs`, `LoggingService.cs`, and future `Systems/Notifications/**` are outside flat `Systems/*.cs` globs.
- **Suggested fix:** Add `Systems/**/*.cs` to CI analyze and `verify-dread.ps1` if extended.

### [SEVERITY: IMPORTANT] No in-game notification system; ROADMAP gap

- **Location:** Entire repo; `docs/ROADMAP.md` Phase 4 (UI-1 only)
- **Category:** design
- **Description:** UI-1 targets shared modal/overlay primitives but does not define toast queue, severity, or non-blocking lifecycle. Without a tracked `NOTIF-1` (or UI-1 sub-task), features may implement ad-hoc `OnGUI` labels or misuse `ErrorReportingPromptSystem` patterns.
- **Suggested fix:** Add ROADMAP row: non-blocking toast host under `Systems/UI/Notifications/`, depends on UI-1 (soft). File GitHub issue with acceptance criteria (queue, max visible, menu-level suppression).

### [SEVERITY: IMPORTANT] `ErrorReportingPromptSystem` is presentation but lives in `ErrorReporting/`

- **Location:** `Systems/ErrorReporting/ErrorReportingPromptSystem.cs`; registry `error-reporting-prompt` in `SystemOrderGroup.Core`
- **Category:** structure
- **Description:** 437 lines IMGUI with no HTTP/queue logic. Folder name suggests future “error notifications” beside `ErrorReporterSystem`. Conflicts with separation of telemetry domain vs player UI (02-ui-review).
- **Suggested fix:** Move to `Systems/UI/ImGui/ErrorReportingPromptSystem.cs` after UI-1 scaffold; keep `ErrorReportingConsent` and `ErrorReportingPrivacyCopy` in `ErrorReporting/`.

### [SEVERITY: IMPORTANT] `ErrorReportingConsent` is telemetry-specific, not a general UI gate

- **Location:** `Systems/ErrorReporting/ErrorReportingConsent.cs:10-18`
- **Category:** design
- **Description:** `IsReportingAllowed()` requires `ErrorReportingEnabled` and `ErrorReportingPromptShown`. Reusing this for other modals or toasts would incorrectly tie unrelated UX to error telemetry cfg.
- **Suggested fix:** When adding notifications, introduce `INotificationPresenter` or `NotificationHost.CanShowNonBlocking()` separate from consent. Document in `domain.md` that consent is ERR-only.

### [SEVERITY: IMPORTANT] Console logging mistaken for player notification risk

- **Location:** `ErrorReporterSystem.cs:40-44`; `LoggingService` used project-wide
- **Category:** maintainability
- **Description:** Enabling error reporting dumps privacy copy to BepInEx log via `LogInfo`. Agents or contributors may assume players were notified in-game. No in-game acknowledgment except the one-time prompt.
- **Suggested fix:** Prefix log line with `[BepInEx only, not shown in game]`. Optional future: one non-blocking toast via notification host when reporting is enabled from cfg without prompt replay (product decision).

### [SEVERITY: IMPORTANT] No `SystemOrderGroup.Ui` for presentation systems

- **Location:** `Systems/DreadSystemRegistry.cs:6-10`
- **Category:** structure
- **Description:** Prompt is `Core` alongside tension and psychotic break. Future `NotificationHost` would either pollute Core or Debug incorrectly.
- **Suggested fix:** Add `SystemOrderGroup.Ui` between Core and Debug; register prompt + notification host there; init order: Core gameplay → Ui → Debug tools.

### [SEVERITY: IMPORTANT] Modal blocks input; notification API must not reuse prompt host

- **Location:** `ErrorReportingPromptSystem.cs:146-157`, `298-351` (input lock); contract `first-run-prompt.md` optional input block
- **Category:** design
- **Description:** Prompt uses per-frame input lock and cursor capture. A toast system reusing the same `MonoBehaviour` would risk leaving input locked or stacking modals.
- **Suggested fix:** Separate hosts: `ModalHost` (blocking, UI-1) vs `NotificationHost` (non-blocking queue). Shared only: theme, `OverlayTextureUtil`, `ImGuiInputCapture` for modals.

### [SEVERITY: NICE_TO_HAVE] `domain.md` ARCH-1 omits messaging surfaces

- **Location:** `docs/agents/domain.md` runtime map
- **Category:** documentation
- **Description:** Lists ErrorReporting and DebugOverlay but not prompt, `LoggingService`, or planned notifications. Increases agent confusion between log and HUD.
- **Suggested fix:** Add rows: `LoggingService` (BepInEx only), `ErrorReportingPromptSystem` (modal), planned `Systems/UI/Notifications/`.

### [SEVERITY: NICE_TO_HAVE] Word “notification” in specs means GitHub, not in-game

- **Location:** `spec/spec-process-cicd-cd.yml.md`, `spec/spec-process-cicd-ci.yml.md` (CI failure notification targets)
- **Category:** documentation
- **Description:** Unrelated to player toasts but grep-noisy for agents searching “notification.”
- **Suggested fix:** One line in `domain.md`: “Notification in CI specs = GitHub alerts, not in-game UI.”

### [SEVERITY: NICE_TO_HAVE] `LoggingService.LogWarning` gated at Debug level

- **Location:** `Systems/LoggingService.cs:68-71`
- **Category:** design
- **Description:** Warnings require `LogLevel.Debug`, same as Info. Operators on `LogLevel.Error` miss warnings (including error-report POST failures). Not a player notification issue but affects operability when diagnosing telemetry.
- **Suggested fix:** Gate `LogWarning` at `LogLevel.Error` per ADR-0014 intent review.

### [SEVERITY: STYLE] High `LoggingService` call volume without structured channels

- **Location:** 26+ files via `rg LoggingService`
- **Category:** maintainability
- **Description:** All messages share `[Dread]` / `[ErrorReporter]` string prefixes manually. No channel enum for filtering (Audio vs Error vs UI).
- **Suggested fix:** Low priority; optional static prefixes or nested loggers when notification host adds `LogUi` traces.

## Positive Patterns

- **Clear telemetry consent gate:** `ErrorReportingConsent` centralizes “no send until prompt dismissed” (ADR-0010, ERR-2 FR-007).
- **Canonical disclosure copy:** `ErrorReportingPrivacyCopy` only; prompt does not paraphrase categories (contract compliance).
- **Menu-level suppression:** Prompt uses `SemiFunc.MenuLevel()` consistent with debug overlay and psychotic break.
- **Logging abstraction:** ADR-0014 removed raw `Plugin.Logger` scatter; runtime level changes via cfg.
- **Registry extensibility:** New hosts added via `DreadSystemRegistry` without `Plugin.cs` spawn lists (ADR-0016).
- **Separation of telemetry and modal rendering:** `ErrorReporterSystem` has no `OnGUI`; only `ErrorReportingPromptSystem` draws player UI for ERR-2.
- **No false notification module:** Absence of half-built toast code avoids dead API surface; greenfield can follow UI-1.

## Design Recommendation

### Should notifications live under UI, standalone, or hybrid?

**Recommendation: hybrid presentation under `Systems/UI/`, domain logic stays in feature folders.**

| Layer | Location | Responsibility |
|-------|----------|----------------|
| **Domain / policy** | `Systems/ErrorReporting/`, `TensionSystem`, etc. | When to signal; no `OnGUI` |
| **Presentation** | `Systems/UI/Notifications/` | Queue, layout, lifetime, z-order |
| **Shared primitives** | `Systems/UI/Shared/` | `OverlayTextureUtil`, UI-1 theme, texture helpers |
| **Blocking modals** | `Systems/UI/ImGui/` | ERR-2 prompt, future confirmations |
| **Operator logs** | `Systems/LoggingService.cs` | BepInEx only; never player toasts |

**Do not** create top-level `Systems/Notifications/` parallel to `Systems/UI/`: that splits theme/shared code and repeats 02-ui-review’s “no `Systems/UI/` governance” problem in a second tree.

**Do not** fold notifications into `ErrorReporting/`: telemetry upload failures are consumers of a notification API, not owners of it.

**Debug overlay is not a notification system**: F10 HUD is persistent instrumentation (DBG-5). Toasts should not piggyback on `DebugOverlayPanel`.

### Modal vs toast decision tree

```
Need player choice or legal disclosure?
  yes -> ModalHost (blocking), ImGui, UI-1
  no  -> NotificationHost (queued, timed), non-blocking
Operator / support diagnosis?
  yes -> LoggingService -> BepInEx log
```

`ErrorReportingPromptSystem` remains **ModalHost** consumer, not `NotificationHost`.

### Migration plan (phased, minimal blast radius)

**Phase 0 (docs only, can ship immediately)**  
- Update `domain.md` ARCH-1: LoggingService, prompt, “no toast system yet.”  
- File ROADMAP/GitHub issue `NOTIF-1` (toast queue + API) depending on UI-1.  
- Cross-link this review from UI-1 issue body.

**Phase 1 (UI-1, ROADMAP P2)**  
- Add `Systems/UI/Shared/` (`DreadImGuiTheme`, `ImGuiTexture` using `OverlayTextureUtil`).  
- Move `ErrorReportingPromptSystem` → `Systems/UI/ImGui/` (namespace unchanged initially).  
- Extract shared input lock to `ImGuiInputCapture` (used by prompt + psychotic break later).

**Phase 2 (notifications scaffold)**  
- Add `Systems/UI/Notifications/NotificationHost.cs` + `INotificationPresenter`.  
- API sketch:
  ```csharp
  public static void Enqueue(string message, NotificationSeverity severity = NotificationSeverity.Info, float durationSeconds = 4f);
  ```
- Register in `DreadSystemRegistry` with new `SystemOrderGroup.Ui`.  
- Rules: no input lock; suppress on `SemiFunc.MenuLevel()`; `GUI.depth` below modals (e.g. 5000 vs prompt 10000).

**Phase 3 (first consumers)**  
- `ErrorReporterSystem`: optional toast on repeated POST failure (config-gated).  
- Defer tension/psychotic break toasts until gameplay design requests them.

**Phase 4 (optional uGUI)**  
- If game-native HUD is required, add `UgNotificationPresenter` implementing same interface; keep queue in host.

**Out of scope for migration:** Changing `LoggingService` to touch UI; using BepInEx desktop notifications (wrong surface for in-run players).

## Recommended Agent Prompts

**P0 (CI):**  
> Extend CI analyze to `Systems/**/*.cs` including `LoggingService.cs` and `ErrorReporting/`. See 01-core-review.

**P1 (ROADMAP):**  
> File issue `NOTIF-1`: non-blocking in-game toast host under `Systems/UI/Notifications/`, depends on UI-1. Acceptance: queue, duration, menu suppress, no `ErrorReporting` import in host.

**P1 (domain docs):**  
> Update `domain.md` ARCH-1 with LoggingService (BepInEx-only), ErrorReportingPrompt (modal), and planned Notifications path. Note CI spec “notification” ≠ in-game.

**P2 (UI-1 + move prompt):**  
> Scaffold `Systems/UI/Shared/` and move `ErrorReportingPromptSystem` to `Systems/UI/ImGui/`. Add `SystemOrderGroup.Ui` and re-register prompt under Ui group. No behavior change.

**P2 (consent clarity):**  
> Document in `ErrorReportingConsent` XML: telemetry-only; do not use for other UI gates.

**P3 (notification host):**  
> Implement `NotificationHost` with `Enqueue` API per Design Recommendation Phase 2. Wire one opt-in consumer (e.g. error POST failure) behind config.

**P3 (logging operability):**  
> Review ADR-0014: gate `LogWarning` at `Error` level; add `[BepInEx only]` prefix on privacy copy log in `ErrorReporterSystem`.

## Cross-references

| Doc | Relevance |
|-----|-----------|
| [01-core-review.md](01-core-review.md) | CI globs; Core compat |
| [02-ui-review.md](02-ui-review.md) | UI folder proposal; prompt vs toast table |
| [ADR-0010](../adr/0010-error-telemetry.md) | Default-on, consent gate, no in-game send before prompt |
| [ADR-0014](../adr/0014-configurable-logging.md) | LoggingService purpose |
| [ADR-0016](../adr/0016-arch-3-extension-model.md) | Registry, init order |
| [domain.md](../agents/domain.md) | ARCH-1 map (needs messaging rows) |
| [ROADMAP.md](../ROADMAP.md) | UI-1, Phase 4 player-facing UI |
| [specs/004-err-2-default-on-prompt/contracts/first-run-prompt.md](../../specs/004-err-2-default-on-prompt/contracts/first-run-prompt.md) | Modal contract, not toast |

### Caller / channel map (rg, 2026-06-01)

| Symbol | In-game player? | Channel |
|--------|-----------------|---------|
| `LoggingService.*` | No | BepInEx `Log.txt` |
| `ErrorReportingPromptSystem.OnGUI` | Yes | Blocking IMGUI modal |
| `DebugOverlayPanel.OnGUI` | Yes (F10) | Non-blocking debug HUD |
| `PsychoticBreakOverlay` | Yes | uGUI vignette |
| `ErrorReportingConsent.IsReportingAllowed` | N/A | Policy (no render) |
| `ErrorReporterSystem` | No | Background HTTP + console logs |

### Severity summary

| Severity | Count |
|----------|------:|
| CRITICAL | 1 |
| IMPORTANT | 6 |
| NICE_TO_HAVE | 3 |
| STYLE | 1 |

**Top 3 concerns:** (1) **no notification system and no ROADMAP issue**, so future features may collide with ErrorReporting or duplicate IMGUI; (2) **channel confusion** between `LoggingService`, debug HUD, modal prompt, and future toasts; (3) **structural debt**: prompt registered as Core under `ErrorReporting/`, no `SystemOrderGroup.Ui`, same CI blind spot as other nested `Systems/**` files.
