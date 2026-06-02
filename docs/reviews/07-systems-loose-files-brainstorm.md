# Feature brainstorm: Systems (root loose files / gameplay)

**Based on:** [07-systems-loose-files-review.md](07-systems-loose-files-review.md)  
**Scope note:** **Features only.** That review proposed Bootstrap/Audio/Tension/Scan folder moves and governance. This addendum targets player and maintainer value from the systems still at `Systems/` root: tension, audio, monsters, scans, and debug server, without additional relocation work.

## Context

Seventeen root files remain after ARCH-1: tension, ambient audio, monster audio treatment, shared enemy cache, boot/registry, and a large debug TCP server. Duplicate `FindObjectsOfType` scans waste CPU; features below assume **one shared scan** as implementation detail while delivering new horror/co-op experiences.

## Feature ideas

### Prioritized summary

| Priority | Name | Effort | Risk | Value |
|----------|------|--------|------|-------|
| 1 | Unified enemy scan consumers | M | Low | Perf + consistency |
| 2 | Tension "breathing room" audio layer | M | Low | Players |
| 3 | Monster audio personality tags | M | Med | Horror read |
| 4 | Ambient dread biome profiles | M | Low | Level variety |
| 5 | Host tension snapshot for co-op | L | High | Co-op design |
| 6 | `DreadRuntimeState` nearest threat bearing | S | Low | UI/MCP |
| 7 | Debug server: subscribe stream (deferred ADR) | L | Med | Agents |
| 8 | Audio clip cache LRU by scene | M | Low | Long sessions |

### Detail

#### Unified enemy scan consumers

- **Value:** Wire `TensionSystem`, `MonsterOverhaulSystem`, and `DebugServerSystem.get_state` to `EnemyScanCache` (and `NearestDistance`). Same gameplay data, fewer allocations. Player benefit: smoother frame times in enemy-heavy scenes.
- **Effort:** M  
- **Dependencies:** `EnemyScanCache`, PERF-1 (soft)  
- **Risk:** Low  
- **Skip if:** PERF-1 profiling shows scan cost negligible.

#### Tension "breathing room" audio layer

- **Value:** When no enemy within panic radius for 60s after tension, play subtle exhale/heartbeat once. Rewards escape without UI meters.
- **Effort:** M  
- **Dependencies:** `TensionSystem`, `AudioClipLoader`, new OGG assets  
- **Risk:** Low  
- **Skip if:** Audio dread already covers calm moments.

#### Monster audio personality tags

- **Value:** Map enemy type names to distinct treatment filters (pitch, reverb) in `MonsterOverhaulSystem`. Players learn threat identity by sound.
- **Effort:** M  
- **Dependencies:** Reflection or type names, audio mixer optional  
- **Risk:** Med (wrong mappings)  
- **Skip if:** All enemies should sound equally alien.

#### Ambient dread biome profiles

- **Value:** Cfg or level-name heuristic selects ambient clip weights (industrial vs organic vs silence). Uses existing weighted pick in `AudioDreadSystem`.
- **Effort:** M  
- **Dependencies:** `AudioDreadSystem`, level detection (`SemiFunc` / scene name)  
- **Risk:** Low  
- **Skip if:** Single global ambient table is intentional.

#### Host tension snapshot for co-op

- **Value:** Host publishes aggregate "team pressure" (max nearest enemy among players) for future UI or audio. Requires network design.
- **Effort:** L  
- **Dependencies:** Photon, ADR-0004 host model, product sign-off  
- **Risk:** High  
- **Skip if:** Dread stays strictly client-local for tension.

#### `DreadRuntimeState` nearest threat bearing

- **Value:** Publishes horizontal angle to nearest valid enemy for optional HUD compass or MCP. Enables UI-2 meter without extra scan.
- **Effort:** S  
- **Dependencies:** `EnemyScanCache`, tension proximity  
- **Risk:** Low  
- **Skip if:** No UI compass planned.

#### Debug server: subscribe stream (deferred ADR)

- **Value:** Long-lived MCP connection or SSE-style tick of `DreadRuntimeState` for agent automation (ADR-0013 deferred commands).
- **Effort:** L  
- **Dependencies:** `DebugServerSystem` refactor, MCP connection pooling  
- **Risk:** Med  
- **Skip if:** Per-tool TCP calls stay fast enough.

#### Audio clip cache LRU by scene

- **Value:** Cap cached clips per scene; evict oldest when exceeding N MB. Prevents multi-hour session memory growth.
- **Effort:** M  
- **Dependencies:** `AudioClipLoader`, scene events  
- **Risk:** Low  
- **Skip if:** Sessions always short.

## Quick wins vs strategic bets

| Quick wins | Strategic bets |
|------------|----------------|
| Scan consolidation | Host team tension snapshot |
| Nearest threat bearing in runtime state | Debug subscribe stream |
| Biome ambient profiles | Monster personality audio map |

## Recommended ROADMAP additions

| ID | Item | Priority | Depends on |
|----|------|----------|------------|
| SCAN-1 | **All features use `EnemyScanCache` (tension, monster, debug)** | P1 | None |
| TENS-1 | **Calm-after-tension relief audio sting** | P2 | AUDIO-1 (done) |
| AUDIO-5 | **Ambient dread biome/level profiles** | P2 | AudioDreadSystem |
| AUDIO-6 | **Per-enemy-type monster audio treatment map** | P3 | MonsterOverhaulSystem |
| RUNTIME-1 | **Nearest threat distance + bearing on `DreadRuntimeState`** | P2 | SCAN-1 |
