# Code Review: Project documentation (docs, specs, CONTEXT, agent hub)

**Reviewed:** 2026-06-02  
**Scope:** All agent-facing and contributor documentation: `README.md`, `THUNDERSTORE_README.md`, `CONTEXT.md`, `AGENTS.md`, `docs/**`, `specs/**`, `spec/`, `.claude/`, `.github/` doc references, and consistency with code after reviews [01](01-core-review.md) through [08](08-dread-mcp-server-review.md).  
**Not in scope:** Line-by-line re-review of C#/TS implementation (covered in section reviews 01-08).

## Executive Summary

Documentation for agents is **strong in the center** (`docs/agents/README.md`, guides, verify runbooks, ADRs, active `specs/`) but **weak at the edges**: no single **structure governance** entry in `AGENTS.md`, **file maps lag ARCH-1/3** (`CONTEXT.md`, `domain.md`, `README.md` project tree), and **stale operator text** (ERR-2 still "active" in `AGENTS.md`, build failure note for `String.Contains` that no longer appears in tree, missing `CONTRIBUTING.md` linked from agent hub).

**Bloat is contained but duplicated:** `docs/agents/archive/superpowers/` (~10 historical plans/specs) plus redirect stubs at `docs/superpowers/README.md` and `docs/agents/archive/README.md`. Safe to keep for archaeology; agents need louder "do not execute" signals at the hub.

**Section reviews 01-08** were not indexed until this pass; they should become the **drift register** for code vs docs. Cross-cutting themes: **CI analyze globs omit `Systems/**/*.cs`**, **half-finished `Systems/` root layout** (17 loose files; governance doc exists but is **unlinked** from `AGENTS.md` / `docs/agents/README.md`), **no in-game notification system** (03), **MCP protocol presentation bugs** (08).

**Review outcome:** ❌ **ISSUES:** docs are usable for experienced contributors but **do not yet govern project structure** for agents without reading 8 review files. P1: wire [systems-folder-governance.md](../agents/systems-folder-governance.md) into the agent hub, refresh file maps, add [reviews/README.md](README.md), fix stale `AGENTS.md` blocks.

## File Structure Assessment

### What works

| Layer | Role | Quality |
|-------|------|---------|
| `CONTEXT.md` | Domain glossary + file map | Strong vocabulary; map stale on `Systems/` |
| `docs/agents/README.md` | Orchestration entry | Clear 5-step path; missing governance + reviews |
| `docs/agents/guides/` | Shipped patterns per system | 11 guides; aligned with ARCH-3 |
| `docs/adr/` | Decisions | 17 ADRs; two `0007-*` filenames (numbering collision) |
| `specs/00N-*` | Speckit contracts for shipped work | 4 feature folders; keep as contract archive |
| `docs/agents/verify-dread.md` + checklist JSON | Tier 0-3 verify | Actionable; matches `scripts/verify-dread.ps1` |
| `docs/reviews/01-08` | Deep section audits | High value; needed master index (added in README) |

### Gaps vs user concerns

| Concern | Finding |
|---------|---------|
| Structure not governed for agents | `systems-folder-governance.md` written 2026-06-02 but **not** in `AGENTS.md` table or `docs/agents/README.md` file index |
| Dead files / bloat | Archive superpowers are dead for execution but not deleted; `docs/superpowers/` is redirect-only; acceptable if hub warns |
| Docs should guide agents on conventions | `mod-architecture.md` step "Create `Systems/YourSystem.cs`" **contradicts** governance rule #1 (no new root loose files) |

### `.specify` / `spec/` / `CONTEXT-MAP.md`

| Path | Status |
|------|--------|
| `.specify/` | **Absent** (no Speckit template tree in repo) |
| `CONTEXT-MAP.md` | **Absent** (single-context repo; OK per `domain.md`) |
| `spec/` | Two CI process notes only (`spec-process-cicd-*.md`); not linked from agent hub |
| `specs/` | Canonical Speckit output (`001` ARCH-2, `002` ARCH-3, `003` ERR-3, `004` ERR-2) |

### Recommended top-level doc layout (agents)

See **Recommended doc structure for agents** below. No new top-level folders required; **reorganize links and maps**, not file mass.

## Doc inventory table

Paths relative to repo root unless noted. **Freshness:** `current` = matches shipped behavior on this branch; `stale` = known drift; `archive` = historical only; `redirect` = pointer elsewhere.

| Path | Purpose | Freshness | Action |
|------|---------|-----------|--------|
| `README.md` | Player + dev overview, boot diagram, project tree | stale | Add `Systems/Core/`, fix loose-file list; remove `HarmonyPatchCompat` from tree line |
| `THUNDERSTORE_README.md` | Package listing copy | current | Keep synced by CD only |
| `CONTEXT.md` | Glossary + file map | stale | Update file map (Core, Bootstrap targets, overlay shipped); fix debug overlay "in development" |
| `AGENTS.md` | Build, release, agent entry | stale | Link governance + reviews; remove/fix SPECKIT ERR-2 block; remove stale CS1501 note if build clean |
| `CHANGELOG.md` | Release notes | current | Agent edits `[Unreleased]` only |
| `LICENSE`, `SECURITY.md` | Legal / disclosure | current | (none) |
| `docs/ROADMAP.md` | Backlog + execution order | current | Mark ERR-2/ARCH-3 done (already); add DOCS-2 for doc governance if filed |
| `docs/agents/README.md` | Agent orchestration hub | stale | Add governance, reviews index, fix CONTRIBUTING link |
| `docs/agents/orchestration.md` | Workflows | current | Cross-link governance when adding systems |
| `docs/agents/domain.md` | ADR consumption, ARCH-1 map | stale | Add `Systems/Core/` row; stop implying flat root is finished |
| `docs/agents/systems-folder-governance.md` | **Placement rules for `Systems/`** | current | **Link from AGENTS.md + agents/README** |
| `docs/agents/guides/README.md` | Guide index | current | Add governance under "Orient in repo" |
| `docs/agents/guides/*.md` (11) | Per-system implementation | mostly current | Patch `harmony-and-patches.md` paths per 04; `mod-architecture.md` new-system steps |
| `docs/agents/verify-dread.md` | Verify tiers | current | (none) |
| `docs/agents/verify-dread-checklist.json` | Machine checklist | current | (none) |
| `docs/agents/error-reporting-test-checklist.md` | ERR-1 manual matrix | current | (none) |
| `docs/agents/issue-tracker.md`, `triage-labels.md` | GitHub workflow | current | (none) |
| `docs/agents/overlay-perf-checklist.md` | PERF-2 manual | current | (none) |
| `docs/agents/archive/README.md` | Archive policy | current | (none) |
| `docs/agents/archive/superpowers/**` | Old plans/specs | archive | Do not execute; keep for archaeology |
| `docs/superpowers/README.md` | Redirect to guides/archive | redirect | No new files here |
| `docs/adr/0001-0016*.md` | Architecture decisions | mostly current | Reconcile ADR-0010 flush transport vs ADR-0015; note duplicate `0007` filenames |
| `docs/mod-compatibility.md` | Player mod compat | current | Linked from guides |
| `docs/repo-config-slider-labels-investigation.md` | REPOConfig UI debug log | current | Reference until DBG-4 |
| `docs/reviews/01-08-*.md` | Section code reviews | current | Use as drift source; indexed in `reviews/README.md` |
| `docs/reviews/README.md` | Master review index | current | Maintain when adding section 10+ |
| `docs/reviews/09-documentation-review.md` | This document | current | (none) |
| `specs/001-arch-2-reduce-reflection/` | ARCH-2 contracts | archive-ish | Contracts still valid for build profiles |
| `specs/002-arch-3-extensible-core/` | Registry lifecycle | current | **Canonical** for new systems |
| `specs/003-err-3-privacy-copy/` | ERR-3 copy contracts | current | Shipped |
| `specs/004-err-2-default-on-prompt/` | ERR-2 + core-enemy-health | current | Shipped; AGENTS SPECKIT block should point to ROADMAP next item |
| `spec/spec-process-cicd-*.md` | CI/CD process notes | stale? | Link from `.github/workflows` or move under `docs/` |
| `.claude/*-prompt.md` | Subagent templates | current | Listed in agents/README |
| `dread-mcp-server/` (no README in tree) | MCP bridge | gap | Optional short `dread-mcp-server/README.md` pointing to `debug-tooling.md` |
| `CONTRIBUTING.md` | PR conventions (cited by agents/README) | **missing** | Create or retarget link to GitHub default / orchestration.md |

## Drift register

Doc or review claim vs code/repo reality. Severity reflects **agent harm** (wrong file placement, wrong verify expectation), not player impact.

| ID | Source | Doc / claim | Reality (code or tree) | Sev |
|----|--------|-------------|-------------------------|-----|
| D-01 | 01,07, governance | CI analyze covers `Systems/**` per AGENTS intent | `ci.yml` greps only `Systems/*.cs` (root) | IMPORTANT |
| D-02 | 01, domain, CONTEXT | ARCH-1 map includes Core | `domain.md` / `CONTEXT.md` omit `Systems/Core/` | IMPORTANT |
| D-03 | CONTEXT | Debug overlay "in development" / not on all branches | `DebugOverlaySystem` registered; `Systems/DebugOverlay/` exists | IMPORTANT |
| D-04 | CONTEXT | "Shared enemy cache" retired; each system own scan | `EnemyScanCache` at `Systems/` root; ADR-0008 | NICE_TO_HAVE |
| D-05 | README, CONTEXT | Project map lists compat at root | `HarmonyPatchCompat` in `Systems/Core/` | NICE_TO_HAVE |
| D-06 | README, CONTEXT, domain | "Other systems: `Systems/*.cs`" | 17 root `.cs` files + subfolders (07 inventory) | IMPORTANT |
| D-07 | mod-architecture | "Create `Systems/YourSystem.cs`" | Governance: no new root loose files | IMPORTANT |
| D-08 | agents/README | `CONTRIBUTING.md` for PR conventions | File **not in repository** | IMPORTANT |
| D-09 | AGENTS.md | Active plan ERR-2 `specs/004-...` | ERR-2 shipped per ROADMAP (#208) | NICE_TO_HAVE |
| D-10 | AGENTS.md | `ErrorReporterSystem` `Contains(..., StringComparison)` breaks build | No match in `ErrorReporting/` on this tree | IMPORTANT |
| D-11 | 06, ADR-0010 | Batch upload via `UnityWebRequest` | `HttpWebRequest` per ADR-0015 | NICE_TO_HAVE |
| D-12 | 03 | Future notifications in `ErrorReporting/` | No notification system; consent is modal only | IMPORTANT |
| D-13 | 08, ADR-0013 | Max request 4096 bytes enforced | `MaxMessageBytes` not checked on read | IMPORTANT |
| D-14 | 08 | MCP `dread_get_logs` text uses camelCase | Unity JSON PascalCase `Level`, `Message` | IMPORTANT |
| D-15 | 08 | MCP `dread_get_patches` text schema | C# uses `prefixes` counts not `patchTypes` | IMPORTANT |
| D-16 | 01 | `PlayerControllerCompat.GetStamina` used | Dead API; error capture uses `player.stamina` | NICE_TO_HAVE |
| D-17 | 01 | `HarmonyPatchCompat.IsMasterClient` fail-closed | Fails open on reflection error | IMPORTANT |
| D-18 | 04 | `harmony-and-patches.md` paths / Apply throws | Log-and-return; verify file paths | NICE_TO_HAVE |
| D-19 | 05, ADR-0011 | Audio names shadow_scream / phantom_footsteps | Shipped `scream_*`, `footsteps.ogg` (CONTEXT historical table OK) | NICE_TO_HAVE |
| D-20 | 02 | `Systems/UI/` exists | Not created; UI split across DebugOverlay, ErrorReporting prompt, PsychoticBreak overlay | NICE_TO_HAVE |
| D-21 | 07 | Error JSON at root should move | `ErrorReportJson.cs`, `ErrorReportTypes.cs` still at `Systems/` root | NICE_TO_HAVE |
| D-22 | 08 | `dist/index.js` committed | In `.gitignore`; build required (git status may show local dist) | STYLE |
| D-23 | docs/adr | Single ADR number 0007 | Two files: `0007-audio-clip-loader`, `0007-ci-pipeline-optimization` | NICE_TO_HAVE |

## Issues

### [SEVERITY: CRITICAL] Agent hub does not point to structure governance

- **Location:** `AGENTS.md` agent table; `docs/agents/README.md` file index
- **Category:** governance
- **Description:** [systems-folder-governance.md](../agents/systems-folder-governance.md) defines golden rules (no new root loose files, registry-only spawn, patch/Core boundaries) but agents starting at `AGENTS.md` never see it. This directly matches the user concern that "project structure is not governed by rules for agents."
- **Suggested fix:** Add rows to both files:
  - `docs/agents/systems-folder-governance.md`: where to place new `Systems/` code
  - `docs/reviews/README.md`: section review index + drift register pointer

### [SEVERITY: IMPORTANT] `CONTEXT.md` file map and debug overlay status are stale

- **Location:** `CONTEXT.md` (File map table; Debug overlay glossary)
- **Category:** drift
- **Description:** Map says runtime systems are flat at `Systems/*.cs` and omits `Systems/Core/`. Debug overlay described as in-development while `DebugOverlaySystem` is in registry and ROADMAP PERF-2/DBG work assumes it exists.
- **Suggested fix:** Replace file map with ARCH-1 + governance target map (Core, Patches, PsychoticBreak, ErrorReporting, DebugOverlay, root loose list with "migrating per 07-review"). Set debug overlay glossary to shipped IMGUI HUD (config-gated).

### [SEVERITY: IMPORTANT] `mod-architecture.md` contradicts folder governance

- **Location:** `docs/agents/guides/mod-architecture.md` ("Adding a new runtime system", step 1)
- **Category:** drift
- **Description:** Instructs `Create Systems/YourSystem.cs` at root. Governance forbids new root loose files and points to feature folders (`Audio/`, `Tension/`, etc.).
- **Suggested fix:** Step 1: create under feature folder per governance table; link `systems-folder-governance.md` and `specs/002-arch-3-extensible-core/contracts/system-lifecycle.md`.

### [SEVERITY: IMPORTANT] Broken link: `CONTRIBUTING.md`

- **Location:** `docs/agents/README.md` line 3
- **Category:** dead-link
- **Description:** "start at CONTRIBUTING.md": file does not exist in repo (grep/find empty).
- **Suggested fix:** Add minimal `CONTRIBUTING.md` (PR title, roadmap ID, verify tier 0, link orchestration) or change link to `docs/agents/orchestration.md` PR section.

### [SEVERITY: IMPORTANT] `AGENTS.md` stale blocks mislead cloud agents

- **Location:** `AGENTS.md` SPECKIT comment; "Known issue on master"
- **Category:** drift
- **Description:** SPECKIT still highlights ERR-2 active plan though ROADMAP marks ERR-2 done. Known issue documents CS1501 for `String.Contains` overload not present in current `ErrorReporting` sources.
- **Suggested fix:** Update SPECKIT to next active roadmap item (e.g. UI-1 / DBG-3) or remove block. Delete or rewrite known-issue section after `dotnet build` on stubs.

### [SEVERITY: IMPORTANT] `domain.md` ARCH-1 table incomplete

- **Location:** `docs/agents/domain.md` Runtime file map
- **Category:** drift
- **Description:** Lists subfolders but not `Systems/Core/`; "Other systems: flat" undermines ARCH-1 completion narrative in ROADMAP.
- **Suggested fix:** Add Core row; replace "flat" with "root loose files (see governance + 07-review) pending folder moves."

### [SEVERITY: IMPORTANT] README project structure tree misleading

- **Location:** `README.md` Project Structure
- **Category:** drift
- **Description:** Shows `HarmonyPatchCompat` under `Systems/` root; omits `Core/`, `ErrorReportJson` at root, `DebugServerSystem` size/location.
- **Suggested fix:** Align tree with governance target or current tree; link `docs/agents/systems-folder-governance.md` for contributors.

### [SEVERITY: IMPORTANT] Section reviews not discoverable from agent entry

- **Location:** `docs/agents/README.md` (before this review)
- **Category:** governance
- **Description:** Reviews 01-08 contain the only consolidated placement and drift guidance; no index linked from hub.
- **Suggested fix:** [reviews/README.md](README.md) (created); link from agents/README and AGENTS.md.

### [SEVERITY: NICE_TO_HAVE] Duplicate / confusing ADR filenames for 0007

- **Location:** `docs/adr/0007-audio-clip-loader.md`, `docs/adr/0007-ci-pipeline-optimization.md`
- **Category:** maintainability
- **Description:** Two ADRs share numeric prefix; agents citing "ADR-0007" are ambiguous.
- **Suggested fix:** Renumber CI doc to 0017+ in a dedicated docs PR (with redirect note in old file).

### [SEVERITY: NICE_TO_HAVE] Archive superpowers volume (~10 files)

- **Location:** `docs/agents/archive/superpowers/`
- **Category:** bloat
- **Description:** Superseded by guides; contains Windows paths and removed systems. Properly labeled in archive README but easy to open from search.
- **Suggested fix:** Add `archive/` to agent "do not read first" callout in `docs/agents/README.md`; optional: collapse to single `ARCHIVE.md` summary + tarball outside repo.

### [SEVERITY: NICE_TO_HAVE] `spec/` CI notes orphaned

- **Location:** `spec/spec-process-cicd-ci.yml.md`, `spec/spec-process-cicd-cd.yml.md`
- **Category:** discoverability
- **Description:** Not linked from `docs/agents/` or `.github/workflows` headers.
- **Suggested fix:** One-line comment in `ci.yml` / `cd.yml` pointing to spec files, or move under `docs/ci/`.

### [SEVERITY: NICE_TO_HAVE] No `dread-mcp-server/README.md`

- **Location:** `dread-mcp-server/`
- **Category:** gap
- **Description:** Package documented only in `docs/agents/guides/debug-tooling.md` and review 08.
- **Suggested fix:** Short README: build, env vars, link to ADR-0013 and review 08.

### [SEVERITY: STYLE] Em dash policy vs existing docs

- **Location:** `AGENTS.md` changelog rules (no em dash)
- **Category:** style
- **Description:** Rule is clear; some older docs may contain `--`. New reviews use hyphen lists consistently.
- **Suggested fix:** Optional grep in CI for ` -- ` in `docs/` when touching files.

## Positive Patterns

- **Single agent entry** with ordered reading list (`docs/agents/README.md`).
- **Guides replaced superpowers** with explicit archive mapping (guides README + archive README).
- **Glossary-first** `CONTEXT.md` reduces synonym drift in issues/PRs.
- **Machine-readable verify** (`verify-dread-checklist.json`) matches human runbook.
- **Speckit specs retained** as contracts for shipped ERR/ARCH work (better than deleting).
- **systems-folder-governance.md** is the right artifact; only discovery is missing.
- **Section reviews 01-08** are thorough, cross-linked, and suitable as an ongoing drift backlog.

## Recommended doc structure for agents

Target: any agent can answer "where does this go?" and "what do I read first?" in under 60 seconds.

```
Read order (always):
  1. AGENTS.md         : build, release, lint, links below
  2. CONTEXT.md        : vocabulary (use in issues/PRs)
  3. docs/agents/README.md
  4. docs/agents/systems-folder-governance.md   ← NEW required link
  5. docs/agents/domain.md + relevant docs/adr/
  6. docs/agents/guides/<area>.md
  7. docs/ROADMAP.md + GitHub issue

When changing code layout or adding Systems:
  docs/reviews/README.md → 07 + governance + 02 (UI moves)

When verifying:
  docs/agents/verify-dread.md (+ checklist JSON)

docs/
  agents/           ← live agent docs only
    README.md       hub (must link governance + reviews)
    systems-folder-governance.md
    guides/         implementation truth
    archive/        read-only history
  adr/              decisions
  reviews/          section audits + drift (README index)
  ROADMAP.md
  mod-compatibility.md, repo-config-slider-labels-investigation.md
  superpowers/      redirect stub only (no new content)

specs/              Speckit contracts (link from plans; do not duplicate in guides)
CONTEXT.md          glossary + file map (update on folder moves)
README.md           players + high-level tree (link governance for devs)
```

**Do not** add a second glossary or orchestration doc. **Do** update `CONTEXT.md` file map whenever governance or 07-review moves land.

## Recommended Agent Prompts

**P0 (discovery):**  
> In `AGENTS.md` and `docs/agents/README.md`, add links to `docs/agents/systems-folder-governance.md` and `docs/reviews/README.md`. Fix or create `CONTRIBUTING.md`.

**P1 (maps):**  
> Refresh `CONTEXT.md` file map and debug overlay glossary; update `domain.md` ARCH-1 table with `Systems/Core/` and root-loose migration note; fix `README.md` project structure.

**P1 (contradictions):**  
> Edit `mod-architecture.md` "Adding a new runtime system" to use feature subfolders and governance; remove SPECKIT ERR-2 active block from `AGENTS.md`; validate/remove known CS1501 issue.

**P2 (drift backlog):**  
> Turn drift register rows D-01, D-13-D-15 into GitHub issues labeled `ready-for-agent` where code fixes are needed; doc-only rows into DOCS-* issues.

**P3 (archive):**  
> Add hub callout: never execute `docs/agents/archive/superpowers/plans/*.md` tasks.

## Cross-references

| Doc | Relevance |
|-----|-----------|
| [reviews/README.md](README.md) | Index of reviews 01-09 + cross-cutting themes |
| [systems-folder-governance.md](../agents/systems-folder-governance.md) | Placement rules (must link from hub) |
| [07-systems-loose-files-review.md](07-systems-loose-files-review.md) | Root file inventory + target folders |
| [01-core-review.md](01-core-review.md) | Core + CI glob |
| [02-ui-review.md](02-ui-review.md) | UI-1 target layout |
| [08-dread-mcp-server-review.md](08-dread-mcp-server-review.md) | MCP + protocol drift |
| [guides/README.md](../agents/guides/README.md) | Live implementation index |
| [ROADMAP.md](../ROADMAP.md) | Shipped vs active work |
