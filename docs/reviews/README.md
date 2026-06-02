# Codebase quality reviews (index)

Structured reviews of the Dread mod repository for agents and maintainers. Each file follows a common shape: executive summary, file structure assessment, severitized issues, recommended prompts, and cross-references.

**When to read this folder:** Before large refactors, when placing new code under `Systems/`, or when docs and code disagree. **Live rules for new code:** [systems-folder-governance.md](../agents/systems-folder-governance.md) (link from [agents/README.md](../agents/README.md) once wired).

**Drift register:** Consolidated doc-vs-code table in [09-documentation-review.md](09-documentation-review.md#drift-register). Section reviews below are the authoritative detail for their scope.

---

## Reviews

| # | Document | One-line summary |
|---|----------|------------------|
| 01 | [01-core-review.md](01-core-review.md) | `Systems/Core/` compat layer: fail-open gates, dead `GetStamina`, CI globs skip nested `Systems/**`. |
| 02 | [02-ui-review.md](02-ui-review.md) | No `Systems/UI/` yet; IMGUI split across debug overlay, ERR-2 prompt, psychotic break uGUI; UI-1 roadmap. |
| 03 | [03-notifications-review.md](03-notifications-review.md) | No notification/toast system; consent is a blocking modal, not `LoggingService`. |
| 04 | [04-patches-review.md](04-patches-review.md) | Four Harmony patches, ADR-0009 lifecycle; foreign-patch skip inconsistent; doc drift in harmony guide. |
| 05 | [05-psychotic-break-review.md](05-psychotic-break-review.md) | Well-scoped partials; lifecycle cleanup gaps; ADR-0011 audio naming drift. |
| 06 | [06-error-reporting-review.md](06-error-reporting-review.md) | Telemetry pipeline sound; sync HTTP on main thread; JSON/types at `Systems/` root. |
| 07 | [07-systems-loose-files-review.md](07-systems-loose-files-review.md) | 17 root `Systems/*.cs` files; duplicate scans; governance doc + phased folder map. |
| 08 | [08-dread-mcp-server-review.md](08-dread-mcp-server-review.md) | MCP bridge usable; protocol/presentation bugs; no TS tests; `MaxMessageBytes` not enforced. |
| 09 | [09-documentation-review.md](09-documentation-review.md) | Docs hub strong; governance unlinked; CONTEXT/README/AGENTS stale; this index + drift register. |

---

## Brainstorm addenda

Follow-up **feature brainstorms** (product capabilities, UX, tooling) keyed to each section review. Scope per file is noted in its header (`features only` = review already covered structure/governance; `full brainstorm` = review was mostly quality/fixes).

| # | Brainstorm | Scope | Review |
|---|------------|-------|--------|
| 01 | [01-core-brainstorm.md](01-core-brainstorm.md) | Full | [01-core-review.md](01-core-review.md) |
| 02 | [02-ui-brainstorm.md](02-ui-brainstorm.md) | Features only | [02-ui-review.md](02-ui-review.md) |
| 03 | [03-notifications-brainstorm.md](03-notifications-brainstorm.md) | Features only | [03-notifications-review.md](03-notifications-review.md) |
| 04 | [04-patches-brainstorm.md](04-patches-brainstorm.md) | Full | [04-patches-review.md](04-patches-review.md) |
| 05 | [05-psychotic-break-brainstorm.md](05-psychotic-break-brainstorm.md) | Full | [05-psychotic-break-review.md](05-psychotic-break-review.md) |
| 06 | [06-error-reporting-brainstorm.md](06-error-reporting-brainstorm.md) | Features only | [06-error-reporting-review.md](06-error-reporting-review.md) |
| 07 | [07-systems-loose-files-brainstorm.md](07-systems-loose-files-brainstorm.md) | Features only | [07-systems-loose-files-review.md](07-systems-loose-files-review.md) |
| 08 | [08-dread-mcp-server-brainstorm.md](08-dread-mcp-server-brainstorm.md) | Features only | [08-dread-mcp-server-review.md](08-dread-mcp-server-review.md) |
| 09 | [09-documentation-brainstorm.md](09-documentation-brainstorm.md) | Features only (agent/docs tooling) | [09-documentation-review.md](09-documentation-review.md) |

When filing GitHub issues, prefer roadmap IDs from the brainstorm **Recommended ROADMAP additions** tables (e.g. `NOTIF-1`, `CORE-2`, `MCP-1`). Do not duplicate shipped ROADMAP rows (`UI-1`, `ERR-4`, `DBG-5`) unless the brainstorm explicitly extends them.

---

## Cross-cutting themes (all sections)

These recur in multiple reviews; fix once, update docs once.

| Theme | Reviews | Typical fix |
|-------|---------|-------------|
| **CI analyze omits nested `Systems/**`** | 01, 02, 04, 05, 06, 07, governance | `ci.yml`: grep `Systems/**/*.cs` (and mirror in `verify-dread.ps1`) |
| **Half-finished ARCH-1 layout** | 07, 09, domain, CONTEXT | Execute folder map in 07; update file maps |
| **Agent structure rules not in hub** | 07, 09 | Link [systems-folder-governance.md](../agents/systems-folder-governance.md) from `AGENTS.md` |
| **No shared UI kit yet** | 02, 03, 06 | ROADMAP UI-1; do not put toasts in `ErrorReporting/` |
| **Compat fail-open / dead APIs** | 01, 05, 06 | Core fail-closed master client; wire or remove `GetStamina` |
| **Debug/MCP protocol drift** | 08, 06 | Fix MCP text formatters; enforce 4096-byte limit in C# |
| **Documentation maps lag code** | 09, 01, 07 | Refresh `CONTEXT.md`, `README.md`, `domain.md` |

---

## Severity rollup (documentation review only)

For code issues, see each section file. [09-documentation-review.md](09-documentation-review.md) doc-specific counts:

| Severity | Count (doc review) | Examples |
|----------|-------------------|----------|
| CRITICAL | 1 | Governance not linked from agent entry |
| IMPORTANT | 7 | Stale CONTEXT/AGENTS, CONTRIBUTING missing, mod-architecture contradicts governance |
| NICE_TO_HAVE | 5 | ADR 0007 duplicate numbers, archive bloat, orphaned `spec/` |
| STYLE | 1 | Em dash policy consistency |

---

## Related agent docs

| Doc | Role |
|-----|------|
| [docs/agents/README.md](../agents/README.md) | Orchestration entry |
| [docs/agents/systems-folder-governance.md](../agents/systems-folder-governance.md) | Where to put new `Systems/` code |
| [docs/agents/guides/README.md](../agents/guides/README.md) | Implementation guides |
| [CONTEXT.md](../../CONTEXT.md) | Glossary |
| [docs/ROADMAP.md](../ROADMAP.md) | Backlog |

---

## Adding review 10+

1. Name file `NN-<area>-review.md` with date and scope in header.
2. Add a row to the table above.
3. Add drift rows to [09-documentation-review.md](09-documentation-review.md) drift register (or split a dedicated drift doc if the register grows too large).
4. Update cross-cutting themes if a new pattern appears in 2+ reviews.
