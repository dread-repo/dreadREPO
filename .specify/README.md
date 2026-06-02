# Spec Kit (`.specify/`) for Dread

This folder configures [Spec Kit](https://github.com/github/spec-kit) style spec-driven development for the Dread R.E.P.O. mod repo.

## What lives here

| Path | Role |
|------|------|
| `feature.json` | Pins the active feature directory (see below) |
| `memory/constitution.md` | Project principles for `/speckit-constitution` and plan gates |
| `memory/project-context.md` | Short agent onboarding pointer |
| `templates/` | Plan, spec, tasks, checklist templates |
| `templates/overrides/` | Optional full-template replacements (see README there) |
| `scripts/bash/` | `setup-plan.sh`, `setup-tasks.sh`, `check-prerequisites.sh` |
| `workflows/` | Spec Kit workflow registry |
| `extensions.yml` | Git extension hooks (auto-commit, feature branch) |

## Active feature pinning

[`feature.json`](./feature.json) field `feature_directory`:

- **Empty string** (`""`): no active Spec Kit feature on `master`. Use when the pinned spec is merged (current default).
- **Non-empty:** path under `specs/` (e.g. `specs/014-remote-audio-assets`) for the feature you are implementing.

When `feature_directory` matches an existing `specs/<dir>/` tree, scripts use that path even if the git branch name differs. Set it when starting a new feature; clear it when the feature merges to `master`.

**Agents:** [AGENTS.md](../AGENTS.md) SPECKIT block describes the implement flow. Shipped specs remain under `specs/` as contracts; do not delete merged folders.

## Bash scripts (run from repo root)

```bash
# Prerequisite check (plan.md, optional tasks)
bash .specify/scripts/bash/check-prerequisites.sh --json

# Resolve paths / copy plan template (destructive to plan.md)
bash .specify/scripts/bash/setup-plan.sh --json

# Tasks phase: list design docs + tasks template path
bash .specify/scripts/bash/setup-tasks.sh --json
```

See script headers for flags. Requires `bash`, `jq` or `python3` for `feature.json` parsing.

## Related docs

- [docs/agents/orchestration.md](../docs/agents/orchestration.md): agent workflows
- [specs/](../specs/): feature specs (002 ARCH-3, 004 ERR-2, 006 lure/snitch, 014 remote audio, etc.)
