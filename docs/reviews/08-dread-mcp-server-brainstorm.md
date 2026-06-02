# Feature brainstorm: dread-mcp-server (agent tooling)

**Based on:** [08-dread-mcp-server-review.md](08-dread-mcp-server-review.md)  
**Scope note:** **Features only.** The MCP review targeted formatter bugs, request size limits, tests, and file splits. Below: new tools and workflows for agents/maintainers, not TypeScript module layout.

## Context

The MCP bridge exposes 11 tools mapping to loopback TCP on `DebugServerSystem`. It is the primary agent interface for verify tiers, config tuning, and destructive test paths (crash, psychotic break). Fixing presentation bugs is prerequisite; these ideas extend agent productivity and safe automation for horror-mod iteration.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Vitest fixtures + formatter tests | M | Low | Regression safety |
| 2 | `dread_watch_runtime` (poll helper) | M | Low | Agents |
| 3 | `dread_set_config_batch` | M | Med | Tuning sessions |
| 4 | Structured JSON default for all tools | S | Low | Agents |
| 5 | `dread_scenario` scripted verify | L | Low | CI Tier 1+ |
| 6 | Episode + tension snapshot tool | S | Low | Feature QA |
| 7 | Error queue stats tool | S | Low | ERR verify |
| 8 | Connection pool / keep-alive | M | Med | Latency |

### Detail

#### Vitest fixtures + formatter tests

- **Value:** Locks log/patch text formatters to Unity PascalCase and C# DTO shapes. Review P1 item framed as capability: trustworthy Tier 1.
- **Effort:** M  
- **Dependencies:** Mock TCP server  
- **Risk:** Low  
- **Skip if:** Manual verify only forever.

#### `dread_watch_runtime` (poll helper)

- **Value:** Single MCP tool that calls `get_runtime_state` every N ms for M seconds, returns min/max/last for tension and psychotic break fields. Agents avoid shell loops.
- **Effort:** M  
- **Dependencies:** MCP SDK, `get_runtime_state`  
- **Risk:** Low  
- **Skip if:** External scripts preferred.

#### `dread_set_config_batch`

- **Value:** Accepts array of `{section,key,value}`; applies sequentially with combined restart warning. Speeds "nightmare preset" setup.
- **Effort:** M  
- **Dependencies:** `set_config` TCP handler  
- **Risk:** Med (partial apply)  
- **Skip if:** Single-key tools are enough.

#### Structured JSON default for all tools

- **Value:** Default `response_format=json` in tool schemas so agents skip broken text mode for logs/patches until formatters fixed.
- **Effort:** S  
- **Dependencies:** Zod tool definitions  
- **Risk:** Low  
- **Skip if:** Text mode fixed first.

#### `dread_scenario` scripted verify

- **Value:** Runs checklist JSON steps (`verify-dread-checklist.json`) with timeouts and pass/fail aggregate. One MCP call for Tier 1.
- **Effort:** L  
- **Dependencies:** Game running, checklist maintenance  
- **Risk:** Low  
- **Skip if:** PowerShell verify script stays canonical.

#### Episode + tension snapshot tool

- **Value:** Thin wrapper returning only psychotic break + tension slice from runtime state (smaller context for LLM).
- **Effort:** S  
- **Dependencies:** `DreadRuntimeState` fields  
- **Risk:** Low  
- **Skip if:** Full runtime state is small enough.

#### Error queue stats tool

- **Value:** Maps to new TCP cmd or existing verify extension for pending errors, consent flags (ERR-5).
- **Effort:** S  
- **Dependencies:** `ErrorReporterSystem`, C# debug server  
- **Risk:** Low  
- **Skip if:** Logs-only diagnosis.

#### Connection pool / keep-alive

- **Value:** Reuse TCP socket across MCP tool calls in one process. Cuts verify latency.
- **Effort:** M  
- **Dependencies:** Protocol stability, id handling  
- **Risk:** Med  
- **Skip if:** Fresh socket per call is acceptable.

## Quick wins vs strategic bets

| Quick wins | Strategic bets |
|------------|----------------|
| JSON default + formatter tests | `dread_scenario` orchestrator |
| Episode/tension snapshot tool | TCP keep-alive pool |
| Error queue MCP | `dread_watch_runtime` |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| MCP-1 | **Vitest suite for line protocol + formatters** | P1 | None |
| MCP-2 | **`dread_get_feature_snapshot` (tension + psychotic break)** | P2 | Runtime state |
| MCP-3 | **`dread_set_config_batch` + restart summary** | P2 | Debug server |
| MCP-4 | **`dread_run_verify_scenario` (checklist driver)** | P3 | verify-dread-checklist.json |
| MCP-5 | **Optional TCP connection reuse in MCP process** | P3 | MCP-1 |
