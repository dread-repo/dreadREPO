# Feature brainstorm: Patches (Harmony gameplay)

**Based on:** [04-patches-review.md](04-patches-review.md)  
**Scope note:** **Full brainstorm.** The patches review emphasized lifecycle, fail-open host gates, and foreign-patch skip parity, with little player-facing vision. Room exists for new horror mechanics behind existing config toggles and ARCH-4 mod API.

## Context

Four Harmony patches tune monster investigation, NavMesh aggression, crouch speed, and debug-console stability. They are host-gated (monsters) or client-local (crouch, console guard). New patch-backed features should respect ADR-0009 apply/remove lifecycle, `CompatibilityMode`, and `HarmonyPatchCompat` foreign-patch skip, while increasing readable threat and co-op fairness.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Patch profile presets (Aggressive / Standard / Safe) | M | Low | Players |
| 2 | Investigate "last known position" bias | M | Med | Horror tension |
| 3 | Crouch audio dampening postfix | S | Low | Stealth fantasy |
| 4 | Monster telegraph cooldown patch | L | High | Fair difficulty |
| 5 | Dynamic patch pack via ARCH-4 | L | Med | Mod ecosystem |
| 6 | Patch telemetry in runtime state | S | Low | MCP/debug |
| 7 | Client-side "heard investigate" cue | M | Low | Audio dread synergy |
| 8 | Harmony skip reason in overlay | S | Low | Support |

### Detail

#### Patch profile presets

- **Value:** One cfg enum sets monster multiplier, investigate scale, and crouch boost together ("Nightmare", "Default", "Subtle") without editing three toggles.
- **Effort:** M  
- **Dependencies:** Existing patch configs, REPOConfig  
- **Risk:** Low  
- **Skip if:** Per-toggle tuning is a deliberate design pillar.

#### Investigate "last known position" bias

- **Value:** Prefix tweak: when director sets investigate radius, slightly bias toward last player noise position (if API exists via reflection). Makes monsters feel like they search, not random expand.
- **Effort:** M  
- **Dependencies:** `EnemyDirectorSetInvestigatePatch`, compat probes, multiplayer host-only  
- **Risk:** Med (desync if mis-gated)  
- **Skip if:** Game API unavailable on target REPO version.

#### Crouch audio dampening postfix

- **Value:** While crouching, lower footstep emit volume on player audio source (client-local). Complements crouch speed boost and psychotic break hide vulnerability.
- **Effort:** S  
- **Dependencies:** `PlayerControllerAwakePatch` or new player patch, `AudioDreadSystem`  
- **Risk:** Low  
- **Skip if:** Game handles crouch audio natively.

#### Monster telegraph cooldown patch

- **Value:** Postfix on enemy attack wind-up to enforce minimum telegraph time on nightmare mode. High effort/risk; needs animation API discovery.
- **Effort:** L  
- **Dependencies:** Reflection inventory, host-only, foreign-patch skip  
- **Risk:** High  
- **Skip if:** Scope stays atmosphere-only (no combat fairness).

#### Dynamic patch pack via ARCH-4

- **Value:** Optional BepInEx soft-dep mods register additional `IPatchPack` entries vetted by Dread semver. Enables community "Dread: Warehouse" packs without forking core DLL.
- **Effort:** L  
- **Dependencies:** ARCH-4, ADR-0009 lifecycle manager  
- **Risk:** Med  
- **Skip if:** Thunderstore single-package model stays fixed.

#### Patch telemetry in runtime state

- **Value:** `DreadRuntimeState` exposes which patches applied, skipped (foreign), or failed apply. Overlay + MCP show patch health.
- **Effort:** S  
- **Dependencies:** `HarmonyPatchCompat`, DBG-5  
- **Risk:** Low  
- **Skip if:** `get_patches` MCP is enough.

#### Client-side "heard investigate" cue

- **Value:** When host investigate patch fires (synced event or heuristic), local player plays rare 3D whisper/click (not toast). Ties monster AI to audio dread.
- **Effort:** M  
- **Dependencies:** `EnemyDirectorSetInvestigatePatch`, `AudioDreadSystem`, network semantics  
- **Risk:** Med  
- **Skip if:** No reliable client-visible signal.

#### Harmony skip reason in overlay

- **Value:** When `ShouldSkipDueToForeignPatches` triggers, push reason string to overlay once per session. Helps "why is aggression off?" support.
- **Effort:** S  
- **Dependencies:** NOTIF-1 or DBG-5, monster patches  
- **Risk:** Low  
- **Skip if:** Warnings in log suffice.

## Quick wins vs strategic bets

| Quick wins | Strategic bets |
|------------|----------------|
| Patch telemetry rows | Investigate position bias |
| Profile presets cfg | ARCH-4 patch packs |
| Skip reason surfacing | Monster telegraph patch |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| PATCH-1 | **Monster/crouch patch profile presets** | P2 | None |
| PATCH-2 | **Runtime patch apply/skip status in `DreadRuntimeState`** | P2 | ARCH-3 (done) |
| PATCH-3 | **Client crouch footstep dampening (Harmony)** | P3 | CompatibilityMode off |
| PATCH-4 | **Investigate-bias postfix (host-only, research spike)** | P3 | ARCH-2 reflection |
