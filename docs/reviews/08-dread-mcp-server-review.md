# Code Review: dread-mcp-server (MCP bridge + DebugServer protocol)

**Reviewed:** 2026-06-02  
**Scope:** `dread-mcp-server/` (TypeScript MCP stdio bridge), its wiring to `DebugServerSystem.cs`, agent docs, and `.cursor/mcp.json`. Cross-refs only for `DebugServerSystem` internals already covered in [07-systems-loose-files-review.md](07-systems-loose-files-review.md) and [02-ui-review.md](02-ui-review.md).  
**Files inventoried:** `dread-mcp-server/src/index.ts`, `package.json`, `package-lock.json`, `tsconfig.json`, `run-mcp.sh`, `.gitignore`; integration: `Systems/DebugServerSystem.cs` (git HEAD), `.cursor/mcp.json`, `scripts/verify-dread.ps1`, `docs/agents/guides/debug-tooling.md`, `docs/agents/verify-dread.md`, `docs/agents/verify-dread-checklist.json`, `docs/agents/README.md`, `docs/adr/0013-debug-server.md`, `CONTEXT.md`, `SECURITY.md`, `.github/dependabot.yml`, `.github/workflows/codeql.yml`

**Prior reviews (not re-reviewed in depth):** [01-core-review.md](01-core-review.md) through [07-systems-loose-files-review.md](07-systems-loose-files-review.md)

## Executive Summary

The MCP bridge is a **thin, stateless stdio adapter**: 11 tools map 1:1 to TCP newline-JSON commands on `127.0.0.1` (default port `15432`). Agent documentation (`debug-tooling.md`, `verify-dread.md`, `README.md`) is **strong and actionable**. Tier 0 verify builds the npm package; CodeQL and Dependabot cover this tree.

Main gaps are **protocol/doc drift**, **presentation bugs in MCP `text` formatters**, and **missing enforcement** of ADR-0013's 4096-byte request limit on the C# side. The TypeScript package has **no unit or integration tests**; correctness is only implied by `npm run build` and manual Tier 1 MCP use. **`dread-mcp-server/` at repo root is the right place** (local dev bridge); `workers/` remains for deployed Cloudflare code (ADR-0010), not MCP.

**Review outcome:** ❌ **ISSUES:** bridge is usable and well-documented for agents, but fix IMPORTANT protocol/presentation mismatches and add lightweight MCP tests before treating Tier 1 verify as fully trustworthy.

## File Structure Assessment

### dread-mcp-server layout

| Path | Role |
|------|------|
| `src/index.ts` | Entire MCP server (~640 lines): TCP client, 11 tools, Zod schemas |
| `dist/index.js` | Build output (required by `.cursor/mcp.json`; not committed) |
| `run-mcp.sh` | Wrapper: fails fast if `dist/` missing, then `node dist/index.js` |
| `package.json` / `package-lock.json` | ESM, Node >=18, MCP SDK + Zod |
| `tsconfig.json` | Strict ES2022, Node16 resolution, declarations + source maps |

**No dead files** in this directory. **No split modules** yet: acceptable for v0.1, but growth should extract `tcpClient.ts` + `tools/*.ts` when adding commands.

### Repo root vs `workers/`

| Location | Purpose | Shipped to players? |
|----------|---------|---------------------|
| `dread-mcp-server/` | Local MCP stdio → loopback TCP (ADR-0013) | No |
| `workers/error-reporter/` | Cloudflare Worker for error ingest (ADR-0010) | Yes (deployed service) |

Keeping MCP at repo root matches `SECURITY.md`, `CONTEXT.md`, and agent hub links. Moving it under `workers/` would confuse contributors (Workers imply Wrangler/deploy). **Recommendation:** stay at root; optional `tools/dread-mcp-server/` only if the repo later groups all dev CLIs.

### Integration map

```
Cursor / agent MCP client
  └─ stdio → dread-mcp-server (Node)
       └─ TCP JSON lines → DebugServerSystem (Unity, loopback)
            └─ Queue → Update() → mod systems / Harmony / config
```

| Consumer | Reference |
|----------|-----------|
| Cursor | `.cursor/mcp.json` → `node …/dread-mcp-server/dist/index.js` |
| Tier 0 CI (local script) | `scripts/verify-dread.ps1` → `npm ci` + `npm run build` in `dread-mcp-server` |
| Agents | `docs/agents/README.md`, `verify-dread.md`, `verify-dread-checklist.json` |
| Domain | `docs/agents/domain.md` → ADR-0013 for remote tooling vs overlay |

## Tool inventory

| MCP tool | TCP `cmd` | Read-only | Destructive | Notes |
|----------|-----------|-----------|-------------|-------|
| `dread_ping` | `ping` | Yes | No | Liveness; reads `data.version`, `data.port` |
| `dread_get_state` | `get_state` | Yes | No | Scene, enemies, player HP/stamina (legacy snapshot) |
| `dread_get_runtime_state` | `get_runtime_state` | Yes | No | `DreadRuntimeState` fields (preferred for features) |
| `dread_get_config` | `get_config` | Yes | No | Flat keys + `sections[]`; optional `section` filter (display name) |
| `dread_set_config` | `set_config` | No | Yes* | `data`: `{ section, key, value }`; restart warnings in `data.warning` |
| `dread_get_patches` | `get_patches` | Yes | No | `data.patches[]` from Harmony |
| `dread_get_logs` | `get_logs` | Yes | No | `data.logs[]` ring buffer |
| `dread_verify` | `verify` | Yes | No | `data.checks[]` health bundle |
| `dread_shutdown` | `shutdown` | No | Yes | Stops TCP listener; game continues |
| `dread_trigger_test_crash` | `trigger_test_crash` | No | **Yes** | Kills game (ERR-1 test path) |
| `dread_force_psychotic_break` | `force_psychotic_break` | No | **Yes** | Starts episode (needs level + system) |

\*Config changes are reversible but can alter gameplay and require restart for debug-server bind keys.

**Env vars (MCP process):**

| Variable | Default | Validation |
|----------|---------|------------|
| `DREAD_HOST` | `127.0.0.1` | None (allows non-loopback target) |
| `DREAD_PORT` | `15432` | 1–65535 or exit 1 |
| `DREAD_TIMEOUT` | `15000` | Min 100 ms or exit 1 |

## Protocol contract notes

### Wire format (aligned)

- **Transport:** TCP to `IPAddress.Loopback` only in game (`DebugServerSystem.Start`).
- **Framing:** One JSON object per line, trailing `\n` (request and response).
- **Request envelope:** `{ "id": number, "cmd": string, "data": object }` (MCP always sends `id: 1`).
- **Success:** `{ "id", "ok": true, "data": … }`.
- **Failure:** `{ "id", "ok": false, "error": string, "code": number }` with codes `-1` generic, `-2` unknown cmd, `-3` invalid params (per ADR-0013).

MCP `sendCommand` writes `JSON.stringify({ id: 1, cmd, data }) + "\n"` and reads **first** line of response. Matches server behavior (one response per queued command).

### Command coverage

All 11 TCP commands in `DebugServerSystem.ExecuteCommand` have MCP tools. No orphan MCP tools. No TCP commands without MCP exposure.

### `set_config` mapping

| MCP args | TCP `data` | C# resolution |
|----------|------------|---------------|
| `section`, `key`, `value` | same | `combinedKey = section` or `section.key` or `key` when bare (`errorReporting` + `key=""`) |

Matches `verify-dread.md` table and `get_config` `debugKey` values (e.g. `debugServer.enabled` → section `debugServer`, key `enabled`).

### Response shape mismatches (MCP presentation)

| Command | C# `data` shape | MCP issue |
|---------|-----------------|-----------|
| `get_patches` | `{ patches: [{ method, prefixes, postfixes, transpilers, finalizers, owners }] }` | **Text** formatter expects `patch.patchTypes.prefixes` (wrong); counts hidden in JSON mode only |
| `get_logs` | `{ logs: [{ Level, Message, Timestamp }] }` | **Text** formatter uses `entry.level` / `entry.message` (Unity serializes **PascalCase** field names) |
| `get_state` | No psychotic-break episode fields | Tool **description** claims episode timer/status (copy-paste drift) |
| `ping` | `{ pong, version, port }` | MCP text ignores `pong`; OK |

### Security model (by design + gaps)

| Control | Game (C#) | MCP (TS) |
|---------|-------------|----------|
| Bind address | `127.0.0.1` only | `DREAD_HOST` defaults to loopback; **can point elsewhere** (useless unless something else listens) |
| Default off | `DebugServerEnabled=false` | N/A |
| Auth | None | None |
| Request size | `MaxMessageBytes=4096` declared | No client-side cap |
| Read timeout | 5s on socket | `DREAD_TIMEOUT` default 15s |
| Command timeout | 10s wait on main thread | Same as socket timeout |
| Destructive cmds | Guarded when server disabled | Same TCP surface |

**Gap:** `MaxMessageBytes` is **never checked** on incoming lines in `DebugServerSystem` (only `line.Length == 0` skip). ADR-0013 and comments claim 4096-byte rejection; implementation does not enforce it.

**Threat model (accepted):** Any local process can call the debug port when enabled. MCP does not expand attack surface beyond TCP; stdio is local to the agent host.

### Operational notes

- **Port fallback:** If configured port is busy, game binds `port+1`; agents must read `dread_ping` or log `LISTENING 127.0.0.1:PORT`.
- **Fresh TCP per tool call:** Simple and correct; higher latency than keep-alive (acceptable for agent verify).
- **`.cursor/mcp.json`:** Invokes `node dist/index.js` directly, not `run-mcp.sh` (weaker error message if build skipped).
- **Build verified:** `npm ci` + `npm run build` succeeds in this worktree (TypeScript 6 / MCP SDK 1.29 / Zod 4).

## Issues

### [SEVERITY: IMPORTANT] `MaxMessageBytes` not enforced on debug server

- **Location:** `Systems/DebugServerSystem.cs` (`MaxMessageBytes` constant vs `ServerLoop` read path)
- **Category:** security / correctness
- **Description:** ADR-0013 and inline constant document a 4096-byte max request; `ReadLine()` accepts arbitrary line length until memory pressure.
- **Suggested fix:** Reject lines where `Encoding.UTF8.GetByteCount(line) > MaxMessageBytes` with `ok:false`, `code:-3`, before enqueue.

### [SEVERITY: IMPORTANT] MCP `dread_get_logs` text format uses wrong JSON keys

- **Location:** `dread-mcp-server/src/index.ts` (`dread_get_logs` text branch, ~451–455)
- **Category:** bug
- **Description:** Server log DTO fields serialize as `Level`, `Message`, `Timestamp` (Unity `JsonUtility`). Text mode reads `entry.level` / `entry.message` / `entry.timestamp`, producing empty levels and messages for agents using `response_format=text`.
- **Suggested fix:** Read PascalCase keys or normalize in `toolCall` handler:
  ```typescript
  const lvl = (entry.Level ?? entry.level) ?? "Info";
  const msg = (entry.Message ?? entry.message) ?? "";
  ```

### [SEVERITY: IMPORTANT] MCP `dread_get_patches` text format expects wrong schema

- **Location:** `dread-mcp-server/src/index.ts` (`dread_get_patches` text branch, ~387–396)
- **Category:** bug
- **Description:** C# returns `prefixes`, `postfixes`, etc. as numeric fields on each patch. Text formatter looks for `patch.patchTypes.prefixes`, so the "Types" line is always omitted.
- **Suggested fix:** Format from `patch.prefixes`, `patch.postfixes`, or drop text mode and document JSON-only for patches.

### [SEVERITY: IMPORTANT] No automated tests for dread-mcp-server

- **Location:** `dread-mcp-server/` (no `test/`, no vitest/jest in `package.json`)
- **Category:** maintainability
- **Description:** Regressions in Zod schemas, TCP framing, or response normalization are caught only by manual MCP use. Main CI workflow does not run npm test for this package (only `verify-dread.ps1` Tier 0 build).
- **Suggested fix:** Add vitest tests with a mock TCP server (line protocol), covering `sendCommand`, error paths, and log/patch formatters. Optional: wire `npm test` into CI or Tier 0 script.

### [SEVERITY: IMPORTANT] Stale / inaccurate tool descriptions in MCP source

- **Location:** `dread-mcp-server/src/index.ts` (`dread_get_state` description, ~159–169)
- **Category:** documentation
- **Description:** Description duplicates `playerHp` / `playerStamina` and mentions episode fields not returned by `get_state` (use `dread_get_runtime_state`).
- **Suggested fix:** Align description with `StateResponse` fields; point episode data to `dread_get_runtime_state`.

### [SEVERITY: NICE_TO_HAVE] ADR-0013 command count and consequences out of date

- **Location:** `docs/adr/0013-debug-server.md` (Commands v1 table vs Consequences "7 commands")
- **Category:** documentation
- **Description:** Implementation and MCP expose 11 commands; ADR body lists them but closing consequences still say "Tightly scoped v1 (7 commands)".
- **Suggested fix:** Update consequences to "11 commands" or version the protocol section.

### [SEVERITY: NICE_TO_HAVE] `DREAD_HOST` not restricted to loopback

- **Location:** `dread-mcp-server/src/index.ts:9`
- **Category:** security
- **Description:** Misconfiguration could target a remote host (game still only listens locally). Low risk but confusing for agents.
- **Suggested fix:** Warn on stderr or refuse hosts other than `127.0.0.1` / `localhost` unless `DREAD_ALLOW_REMOTE=1`.

### [SEVERITY: NICE_TO_HAVE] MCP does not surface TCP `code` on errors

- **Location:** `dread-mcp-server/src/index.ts` (`toolCall`, ~93–94)
- **Category:** ergonomics
- **Description:** Failed `ok:false` responses only expose `response.error` string; agents cannot branch on `-2` vs `-3` programmatically without parsing text.
- **Suggested fix:** Include `code` in error text or structured JSON for tool errors.

### [SEVERITY: NICE_TO_HAVE] Hardcoded request `id: 1`

- **Location:** `dread-mcp-server/src/index.ts:43`
- **Category:** design
- **Description:** Fine for single-flight stdio tools; prevents future multiplexing on one TCP connection.
- **Suggested fix:** Monotonic id per process if connection pooling is added later.

### [SEVERITY: NICE_TO_HAVE] `.cursor/mcp.json` bypasses `run-mcp.sh`

- **Location:** `.cursor/mcp.json` vs `run-mcp.sh`
- **Category:** ergonomics
- **Description:** `run-mcp.sh` prints a clear build hint; Cursor config calls `dist/index.js` directly and fails opaquely if dist is missing.
- **Suggested fix:** Point args at `run-mcp.sh` or add a preflight in docs to run Tier 0 verify first.

### [SEVERITY: NICE_TO_HAVE] Main CI workflow does not build dread-mcp-server

- **Location:** `.github/workflows/ci.yml` (Node job only for `workers/error-reporter`)
- **Category:** CI
- **Description:** Tier 0 `verify-dread.ps1` builds MCP locally but default PR CI may not run that script on every path.
- **Suggested fix:** Add a small `mcp-build` job or include `npm ci && npm run build` in existing analyze/build workflow.

### [SEVERITY: NICE_TO_HAVE] Single-file MCP server growth

- **Location:** `dread-mcp-server/src/index.ts` (~640 lines)
- **Category:** structure
- **Description:** All tools inline; harder to review diffs per command.
- **Suggested fix:** Split when adding v2 commands (`inspect`, `subscribe` per ADR deferred list).

### Cross-reference: C# debug server (not re-reviewed)

See [07-systems-loose-files-review.md](07-systems-loose-files-review.md) for `DebugServerSystem` god-file (~1k lines), duplicate `FindObjectsOfType` in `get_state`, and move to `Systems/Debug/`.

## Positive Patterns

- **1:1 MCP ↔ TCP mapping** with Zod `strictObject` on tool inputs (rejects unknown fields).
- **Tool annotations** (`readOnlyHint`, `destructiveHint`) on destructive tools (`dread_shutdown`, crash, psychotic break).
- **Env validation** for port and timeout at startup with clear stderr + exit code.
- **Stateless TCP client** per call: no stale connection state; simple failure modes.
- **Agent runbooks** are excellent: `debug-tooling.md`, `verify-dread.md`, checklist JSON `mcp_sequence`, `set_config_examples`.
- **Dependabot** group for `@modelcontextprotocol/*` and **CodeQL** includes `dread-mcp-server/**`.
- **Default-off debug server** in `DreadConfig` with restart-required bind keys documented for agents.
- **Strict TypeScript** (`strict: true`, ES2022, source maps) and ESM aligned with Node 18+.

## Recommended Agent Prompts

**P0 (log text format):**  
> Fix `dread_get_logs` and `dread_get_patches` text formatters to match Unity JSON field names. Add vitest tests with fixture TCP responses.

**P1 (C# size cap):**  
> Enforce `MaxMessageBytes` in `DebugServerSystem.ServerLoop` before enqueue; return `code:-3` for oversized lines. Add a one-line note in ADR-0013 if behavior changes.

**P1 (MCP tests):**  
> Add `dread-mcp-server` vitest suite and `npm test` script; optionally hook into `verify-dread.ps1` Tier 0 after build.

**P2 (docs):**  
> Fix `dread_get_state` tool description; update ADR-0013 consequences command count; consider `run-mcp.sh` in `.cursor/mcp.json`.

**P2 (CI):**  
> Add GitHub Actions step to `npm ci && npm run build` in `dread-mcp-server` on PRs touching `src/` or `package-lock.json`.

## Cross-references

| Doc | Relevance |
|-----|-----------|
| [ADR-0013](../adr/0013-debug-server.md) | TCP protocol, security, MCP companion architecture |
| [debug-tooling.md](../agents/guides/debug-tooling.md) | Commands, MCP build, `DreadRuntimeState` |
| [verify-dread.md](../agents/verify-dread.md) | Tier 0–1 MCP sequence, `set_config` keys |
| [verify-dread-checklist.json](../agents/verify-dread-checklist.json) | Machine-readable `mcp_sequence` |
| [domain.md](../agents/domain.md) | Prefer debug server over overlay for remote tooling |
| [07-systems-loose-files-review.md](07-systems-loose-files-review.md) | `DebugServerSystem` structure and duplicate scans |
| [06-error-reporting-review.md](06-error-reporting-review.md) | `dread_trigger_test_crash` / ERR-1 matrix |
| [SECURITY.md](../../SECURITY.md) | Scope: MCP not shipped to players |

## Severity summary

| Severity | Count |
|----------|------:|
| CRITICAL | 0 |
| IMPORTANT | 5 |
| NICE_TO_HAVE | 7 |
| STYLE | 0 |

## Review outcome

❌ **ISSUES:** MCP bridge and agent docs are production-quality for onboarding; fix IMPORTANT formatter bugs and request-size enforcement, then add minimal TCP/MCP tests so Tier 1 verify is regression-safe.
