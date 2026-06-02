# Code Review: Project documentation (docs, specs, CONTEXT, agent hub)

**Reviewed:** 2026-06-02  
**Reconciled:** 2026-06-03 (post-`master` doc passes #235, #244; AUDIO-5 #237; reviews #242)  
**Scope:** All agent-facing and contributor documentation: `README.md`, `THUNDERSTORE_README.md`, `CONTEXT.md`, `AGENTS.md`, `docs/**`, `specs/**`, `spec/`, `.specify/`, `.claude/`, `.github/` doc references, and consistency with code after reviews [01](01-core-review.md) through [08](08-dread-mcp-server-review.md).  
**Not in scope:** Line-by-line re-review of C#/TS implementation (covered in section reviews 01-08).

## Reconciliation (2026-06-03)

Doc-only fixes on `master` addressed many P0/P1 items from this review:

| Item | Status |
|------|--------|
| Governance + reviews linked from `AGENTS.md` / `docs/agents/README.md` | **Resolved** (#242) |
| Ten core systems + `AudioAssetSystem`; remote `audio-cache` (README, THUNDERSTORE, CONTEXT, mod-architecture) | **Resolved** (#237, #244) |
| SPECKIT / known-issue stale blocks in `AGENTS.md` | **Resolved** (#235, #244) |
| `mod-architecture` new-system step vs governance | **Resolved** (#244) |
| `CONTRIBUTING.md` exists; verify command fixed | **Resolved** (#235) |
| `dread-mcp-server/README.md` | **Resolved** (#235) |
| `.specify/feature.json` cleared after 014 merge; `.specify/README.md` updated | **Resolved** (follow-up) |
| ADR-0006 marked superseded by ADR-0017 | **Resolved** (follow-up) |
| `domain.md` ARCH-1 map (`Core/`, `AudioAssets/`, notifications) | **Resolved** (follow-up) |

**Still open (code or section reviews):** CI analyze glob (D-01), MCP protocol drift (D-13 to D-15), root loose `Systems/*.cs` migration (D-06, 07-review), section review 03 still predates `DreadNotificationSystem` (update 03 separately).

## Executive Summary

Documentation for agents is **strong at the hub** (`docs/agents/README.md`, guides including `remote-assets.md`, governance, verify runbooks, ADRs 0016/0017, shipped `specs/`). **Remaining gaps** are mostly **code drift** (CI globs, MCP JSON shape, `Systems/` folder migration) tracked in the drift register below and in reviews 01-08, not missing hub links.

**Bloat is contained:** `docs/agents/archive/superpowers/` and `docs/superpowers/` redirects stay for archaeology; hub and archive banners say do not execute.

**Review outcome (2026-06-03):** Hub and registry docs are **current for AUDIO-5 and ARCH-3**. Use this file as the **doc drift register**; use section reviews for **implementation** drift. Re-run a full doc inventory after the next major feature merge.

## File Structure Assessment

### What works

| Layer | Role | Quality |
|-------|------|---------|
| `CONTEXT.md` | Domain glossary + file map | Strong vocabulary; map includes AudioAssets (2026-06) |
| `docs/agents/README.md` | Orchestration entry | Governance + reviews linked (#242) |
| `docs/agents/guides/` | Shipped patterns per system | 12+ guides incl. `remote-assets.md`, `ui-notifications.md` |
| `docs/adr/` | Decisions | 17 ADRs; two `0007-*` filenames (numbering collision) |
| `specs/00N-*` | Speckit contracts for shipped work | 4 feature folders; keep as contract archive |
| `docs/agents/verify-dread.md` + checklist JSON | Tier 0-3 verify | Actionable; matches `scripts/verify-dread.ps1` |
| `docs/reviews/01-08` | Deep section audits | High value; needed master index (added in README) |

### Gaps vs user concerns

| Concern | Finding |
|---------|---------|
| Structure not governed for agents | **Resolved:** governance linked from hub (#242); `mod-architecture` points to governance (#244) |
| Dead files / bloat | Archive superpowers are dead for execution but not deleted; `docs/superpowers/` is redirect-only; acceptable if hub warns |
| Docs should guide agents on conventions | **Resolved** in `mod-architecture.md` (#244); 07-review migration still open in code |

### `.specify` / `spec/` / `CONTEXT-MAP.md`

| Path | Status |
|------|--------|
| `.specify/` | Present (Spec Kit templates, scripts, `feature.json`; empty pin = no active feature) |
| `CONTEXT-MAP.md` | **Absent** (single-context repo; OK per `domain.md`) |
| `spec/` | Two CI process notes only (`spec-process-cicd-*.md`); not linked from agent hub |
| `specs/` | Shipped contracts (`001` ARCH-2, `002` ARCH-3, `003` ERR-3, `004` ERR-2, `006` lure/snitch, `012` strip debug, `014` remote audio, etc.) |

### Recommended top-level doc layout (agents)

See **Recommended doc structure for agents** below. No new top-level folders required; **reorganize links and maps**, not file mass.

## Doc inventory table

Paths relative to repo root unless noted. **Freshness:** `current` = matches shipped behavior on this branch; `stale` = known drift; `archive` = historical only; `redirect` = pointer elsewhere.

| Path | Purpose | Freshness | Action |
|------|---------|-----------|--------|
| `README.md` | Player + dev overview, boot diagram, project tree | current | AUDIO-5 tree + `audio-cache` install (#244); revisit when 07-review moves land |
| `THUNDERSTORE_README.md` | Package listing copy | current | First-run audio note (#244); CD sync only |
| `CONTEXT.md` | Glossary + file map | current | Ten core + audio assets; overlay dev-only (#244) |
| `AGENTS.md` | Build, release, agent entry | current | Governance, remote audio, SPECKIT 014 merged note (#235/#244) |
| `CHANGELOG.md` | Release notes | current | Agent edits `[Unreleased]` only |
| `LICENSE`, `SECURITY.md` | Legal / disclosure | current | (none) |
| `docs/ROADMAP.md` | Backlog + execution order | current | Mark ERR-2/ARCH-3 done (already); add DOCS-2 for doc governance if filed |
| `docs/agents/README.md` | Agent orchestration hub | current | Governance + reviews (#242) |
| `docs/agents/orchestration.md` | Workflows | current | Cross-link governance when adding systems |
| `docs/agents/domain.md` | ADR consumption, ARCH-1 map | current | Core, AudioAssets, notifications (#244 follow-up) |
| `docs/agents/systems-folder-governance.md` | **Placement rules for `Systems/`** | current | Linked from hub |
| `docs/agents/guides/README.md` | Guide index | current | `remote-assets.md`, governance in orient row |
| `docs/agents/guides/*.md` | Per-system implementation | mostly current | `harmony-and-patches.md` paths per 04-review still optional |
| `docs/agents/verify-dread.md` | Verify tiers | current | (none) |
| `docs/agents/verify-dread-checklist.json` | Machine checklist | current | (none) |
| `docs/agents/error-reporting-test-checklist.md` | ERR-1 manual matrix | current | (none) |
| `docs/agents/issue-tracker.md`, `triage-labels.md` | GitHub workflow | current | (none) |
| `docs/agents/overlay-perf-checklist.md` | PERF-2 manual | current | (none) |
| `docs/agents/archive/README.md` | Archive policy | current | (none) |
| `docs/agents/archive/superpowers/**` | Old plans/specs | archive | Do not execute; keep for archaeology |
| `docs/superpowers/README.md` | Redirect to guides/archive | redirect | No new files here |
| `docs/adr/0001-0017*.md` | Architecture decisions | mostly current | ADR-0006 superseded by 0017; duplicate `0007-*` filenames remain |
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
| `dread-mcp-server/README.md` | MCP bridge | current | Points to `debug-tooling.md` (#235) |
| `CONTRIBUTING.md` | PR conventions | current | Linked from agents/README |
| `.specify/feature.json` | Active Spec Kit pin | current | Empty after 014 merge; set when starting next feature |

## Drift register

Doc or review claim vs code/repo reality. Severity reflects **agent harm** (wrong file placement, wrong verify expectation), not player impact.

| ID | Source | Doc / claim | Reality (code or tree) | Sev | Status |
|----|--------|-------------|-------------------------|-----|--------|
| D-01 | 01,07, governance | CI analyze covers `Systems/**` per AGENTS intent | `ci.yml` greps only `Systems/*.cs` (root) | IMPORTANT | open |
| D-02 | 01, domain, CONTEXT | ARCH-1 map includes Core | `domain.md` lists `Systems/Core/` | IMPORTANT | resolved |
| D-03 | CONTEXT | Debug overlay "in development" | Dev-build only; glossary updated | IMPORTANT | resolved |
| D-04 | CONTEXT | "Shared enemy cache" retired | `ProximityScan` in `Systems/Core/` | NICE_TO_HAVE | resolved |
| D-05 | README, CONTEXT | Project map lists compat at root | README tree updated (#244) | NICE_TO_HAVE | resolved |
| D-06 | README, CONTEXT, domain | Root loose `Systems/*.cs` | Still true; governance + 07-review | IMPORTANT | open |
| D-07 | mod-architecture | New system at root | Governance link in step 1 | IMPORTANT | resolved |
| D-08 | agents/README | `CONTRIBUTING.md` missing | File exists | IMPORTANT | resolved |
| D-09 | AGENTS.md | Active plan ERR-2 | SPECKIT notes 014 merged | NICE_TO_HAVE | resolved |
| D-10 | AGENTS.md | CS1501 `Contains` known issue | Removed from AGENTS | IMPORTANT | resolved |
| D-11 | 06, ADR-0010 | Batch upload via `UnityWebRequest` | `HttpWebRequest` per ADR-0015 | NICE_TO_HAVE | open |
| D-12 | 03 | No notification system | `DreadNotificationSystem` shipped | IMPORTANT | resolved (update 03-review) |
| D-13 | 08, ADR-0013 | Max request 4096 bytes enforced | `MaxMessageBytes` not checked on read | IMPORTANT | open |
| D-14 | 08 | MCP `dread_get_logs` text uses camelCase | Unity JSON PascalCase `Level`, `Message` | IMPORTANT | open |
| D-15 | 08 | MCP `dread_get_patches` text schema | C# uses `prefixes` counts not `patchTypes` | IMPORTANT | open |
| D-16 | 01 | `PlayerControllerCompat.GetStamina` used | Dead API; error capture uses `player.stamina` | NICE_TO_HAVE | open |
| D-17 | 01 | `HarmonyPatchCompat.IsMasterClient` fail-closed | Fails open on reflection error | IMPORTANT | open |
| D-18 | 04 | `harmony-and-patches.md` paths / Apply throws | Log-and-return; verify file paths | NICE_TO_HAVE | open |
| D-19 | 05, ADR-0011 | Audio names shadow_scream / phantom_footsteps | Shipped `scream_*`, `footsteps.ogg` (CONTEXT historical table OK) | NICE_TO_HAVE | resolved |
| D-20 | 02 | `Systems/UI/` exists | Not created; UI split across DebugOverlay, ErrorReporting prompt, PsychoticBreak overlay | NICE_TO_HAVE | open |
| D-21 | 07 | Error JSON at root should move | `ErrorReportJson.cs`, `ErrorReportTypes.cs` still at `Systems/` root | NICE_TO_HAVE | open |
| D-22 | 08 | `dist/index.js` committed | In `.gitignore`; build required (git status may show local dist) | STYLE | open |
| D-23 | docs/adr | Single ADR number 0007 | Two files: `0007-audio-clip-loader`, `0007-ci-pipeline-optimization` | NICE_TO_HAVE | open |
| D-24 | AUDIO-5 | Bundled `audio/` in Thunderstore zip | DLL-only + GitHub Release OGG | IMPORTANT | resolved |
| D-25 | registry | Nine core systems in docs | Ten core (`AudioAssetSystem`) | IMPORTANT | resolved (#244) |

## Issues

### [SEVERITY: CRITICAL] Agent hub does not point to structure governance

- **Status:** **Resolved** (2026-06-03). Linked from `AGENTS.md` and `docs/agents/README.md` (#242).
- **Location:** `AGENTS.md` agent table; `docs/agents/README.md` file index
- **Category:** governance
- **Description:** Was: governance doc not linked from hub.
- **Suggested fix:** (done) `systems-folder-governance.md` + `docs/reviews/README.md` in hub tables.

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
