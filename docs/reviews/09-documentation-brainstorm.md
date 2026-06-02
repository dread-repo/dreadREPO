# Feature brainstorm: Documentation (agent + contributor)

**Based on:** [09-documentation-review.md](09-documentation-review.md)  
**Scope note:** **Features only (maintainer/agent product).** The documentation review focused on hub links, drift registers, folder governance discovery, and stale maps, not player-facing docs. Below: tooling and doc products that reduce drift and speed agents, without proposing another doc tree reorganization.

## Context

Dread's doc stack (`CONTEXT.md`, `docs/agents/`, ADRs, `specs/`, section reviews 01-09) is strong at the center but weak at the edges: governance unlinked, maps lag code, MCP/CI drift undocumented. "Features" here mean **automation and interactive aids** for humans and agents building a co-op horror mod.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Doc drift CI job (D-* register) | M | Low | Agents + CI |
| 2 | Auto-generated file map snippet | M | Low | CONTEXT freshness |
| 3 | `CONTRIBUTING.md` + issue templates | S | Low | Contributors |
| 4 | MCP tool: `dread_doc_drift` (read-only) | M | Low | Agents |
| 5 | Review-driven ROADMAP seed script | S | Low | Planning |
| 6 | Player-facing "What's Dread" mini-site | L | Med | Players |
| 7 | ADR index with full-text search | S | Low | Agents |
| 8 | Changelog ↔ ROADMAP linker in verify | S | Low | Releases |

### Detail

#### Doc drift CI job (D-* register)

- **Value:** CI step fails or warns on known drift IDs from [09-documentation-review.md](09-documentation-review.md#drift-register) (e.g. `HarmonyPatchCompat` path in README, CI glob claim). Keeps reviews alive.
- **Effort:** M  
- **Dependencies:** `scripts/verify-dread.ps1` or new `scripts/verify-docs.ps1`  
- **Risk:** Low  
- **Skip if:** Manual review cadence is enough.

#### Auto-generated file map snippet

- **Value:** Script emits `Systems/` tree + registry ids into `CONTEXT.md` block comment or `docs/generated/file-map.md` on release. Agents read generated truth.
- **Effort:** M  
- **Dependencies:** `DreadSystemRegistry`, governance paths  
- **Risk:** Low  
- **Skip if:** Hand-maintained map is updated every ARCH PR.

#### `CONTRIBUTING.md` + issue templates

- **Value:** Fixes broken agents/README link; encodes roadmap ID in PR body, verify tier 0, governance link. Review P0 item as deliverable feature.
- **Effort:** S  
- **Dependencies:** GitHub templates  
- **Risk:** Low  
- **Skip if:** orchestration.md alone is canonical.

#### MCP tool: `dread_doc_drift` (read-only)

- **Value:** Returns JSON of live registry paths vs `domain.md` ARCH-1 list (parse both). Agents self-check before moving files.
- **Effort:** M  
- **Dependencies:** MCP, optional C# `list_systems` debug cmd  
- **Risk:** Low  
- **Skip if:** Doc drift CI covers all cases.

#### Review-driven ROADMAP seed script

- **Value:** Parses `docs/reviews/*-brainstorm.md` ROADMAP tables and prints GitHub issue bodies (labels `idea`, roadmap id). Speeds filing NOTIF-1, CORE-1, etc.
- **Effort:** S  
- **Dependencies:** Brainstorm files exist  
- **Risk:** Low  
- **Skip if:** Issues filed manually.

#### Player-facing "What's Dread" mini-site

- **Value:** Static GitHub Pages from README + CONTEXT glossary (player-safe subset). Thunderstore players learn tension vs break vs compat mode.
- **Effort:** L  
- **Dependencies:** Docs maintenance budget  
- **Risk:** Med (duplicate Thunderstore)  
- **Skip if:** README suffices.

#### ADR index with full-text search

- **Value:** `docs/adr/README.md` table with one-line decision summary + links; optional ripgrep helper in verify. Faster than opening 17 files.
- **Effort:** S  
- **Dependencies:** None  
- **Risk:** Low  
- **Skip if:** domain.md ADR table is enough.

#### Changelog ↔ ROADMAP linker in verify

- **Value:** Tier 0 warns when `[Unreleased]` mentions a roadmap ID still marked `idea` in ROADMAP.md. Prevents release notes for unshipped work.
- **Effort:** S  
- **Dependencies:** `verify-dread.ps1`  
- **Risk:** Low  
- **Skip if:** CD pipeline already enforces.

## Quick wins vs strategic bets

| Quick wins | Strategic bets |
|------------|----------------|
| CONTRIBUTING.md | Doc drift CI |
| ADR index README | Auto-generated file map |
| ROADMAP seed script | Player mini-site |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| DOCS-2 | **Link governance + reviews from AGENTS.md and agents/README** | P1 | None (doc edit) |
| DOCS-3 | **CONTRIBUTING.md + PR template with roadmap ID** | P1 | None |
| DOCS-4 | **Doc drift checks in Tier 0 verify (select D-* rows)** | P2 | verify-dread.ps1 |
| DOCS-5 | **Generated `Systems/` file map on release** | P3 | CD pipeline |
| DOCS-6 | **ADR index README with decision summaries** | P3 | None |
