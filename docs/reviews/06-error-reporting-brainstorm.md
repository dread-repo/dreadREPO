# Feature brainstorm: Error reporting (telemetry)

**Based on:** [06-error-reporting-review.md](06-error-reporting-review.md)  
**Scope note:** **Features only.** The error-reporting review covered pipeline reliability, JSON placement, and prompt/UI boundaries. Below: product and operator capabilities, not moving `ErrorReportJson.cs` or restructuring folders.

## Context

ERR-1/2/3 shipped: default-on reporting, first-run consent, privacy copy, manual JSON, Cloudflare Worker to GitHub. Remaining pain is main-thread flush, silent queue loss, and player unawareness when uploads fail. Ideas leverage Worker, MCP test crash, debug overlay, and future NOTIF-1 without turning telemetry into a gameplay system.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Non-blocking batch flush (ERR-4) | M | Med | Players (no hitch) |
| 2 | Upload failure toast | S | Low | Opted-in players |
| 3 | Offline queue + retry badge | M | Med | Reliability |
| 4 | "Copy report ID" in consent modal | S | Low | Support |
| 5 | Redacted live issue link (host only) | M | Med | Power users |
| 6 | MCP `dread_get_error_queue_stats` | S | Low | Agents |
| 7 | Session summary in overlay | S | Low | Developers |
| 8 | Beta channel / staged Worker URL | M | Low | Maintainers |

### Detail

#### Non-blocking batch flush (ERR-4)

- **Value:** Moves POST off main thread or uses UWR when available. Eliminates up-to-15s freezes during error storms.
- **Effort:** M (already ROADMAP ERR-4)  
- **Dependencies:** `UnityWebRequestCompat`, `ErrorReportUploader`  
- **Risk:** Med (Unity thread rules)  
- **Skip if:** Flush rate stays very low in production.

#### Upload failure toast

- **Value:** See NOTIF-2; one non-blocking message when batch fails N times.
- **Effort:** S  
- **Dependencies:** NOTIF-1, ERR-4  
- **Risk:** Low  
- **Skip if:** Privacy policy forbids any in-game telemetry messaging beyond consent.

#### Offline queue + retry badge

- **Value:** Persist last failed batch to disk (encrypted optional); retry on next level load. Overlay shows pending count.
- **Effort:** M  
- **Dependencies:** ERR-4, ADR-0010 persistence decision  
- **Risk:** Med (disk privacy)  
- **Skip if:** Product accepts lossy telemetry.

#### "Copy report ID" in consent modal

- **Value:** After first successful upload in session, modal footer offers copyable hash for Discord support (no PII).
- **Effort:** S  
- **Dependencies:** UI-1 buttons, Worker response hash  
- **Risk:** Low  
- **Skip if:** GitHub issues are internal-only.

#### Redacted live issue link (host only)

- **Value:** Cfg-gated button opens browser to GitHub issue if Worker returns `issue_url` (public repo). Helps open-source community.
- **Effort:** M  
- **Dependencies:** Worker API change, security review  
- **Risk:** Med (leaks repo structure)  
- **Skip if:** Issues stay private.

#### MCP `dread_get_error_queue_stats`

- **Value:** Returns pending count, last flush time, last error, consent flags. Tier 1 verify without parsing logs.
- **Effort:** S  
- **Dependencies:** `ErrorReporterSystem`, `DebugServerSystem`  
- **Risk:** Low  
- **Skip if:** `get_runtime_state` extended instead.

#### Session summary in overlay

- **Value:** DBG-5 section: reports sent this session, dropped (queue full), last POST ms.
- **Effort:** S  
- **Dependencies:** DBG-5, reporter counters  
- **Risk:** Low  
- **Skip if:** MCP-only operators.

#### Beta channel / staged Worker URL

- **Value:** Optional cfg `ErrorReportingEndpoint` for staging Worker; default production URL unchanged. Speeds MCP/ERR verification.
- **Effort:** M  
- **Dependencies:** `ErrorReportUploader`, ops docs  
- **Risk:** Low  
- **Skip if:** Manual DLL swap is rare.

## Quick wins vs strategic bets

| Quick wins | Strategic bets |
|------------|----------------|
| MCP queue stats | ERR-4 non-blocking flush |
| Overlay session counters | Disk-backed offline queue |
| Copy report ID UX | Public issue link |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| ERR-5 | **MCP + overlay error queue statistics** | P2 | DBG-5 (soft) |
| ERR-6 | **Optional disk-backed failed batch retry** | P3 | ERR-4 |
| ERR-7 | **Support hash copy in post-consent UI** | P3 | UI-1 |

*ERR-4 remains the primary reliability bet; NOTIF-2 covers player-visible failures.*
