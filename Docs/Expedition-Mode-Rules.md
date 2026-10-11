# Additional expedition mode rules

These pure rules implement three optional arena modes alongside the ordinary dungeon. They do not unlock ordinary dungeon tiers, consume or change ordinary dungeon blessing choices, alter permanent combat stats, spawn Unity objects, or write saves. Runtime entry, spawning, encounter behaviour, world layout, pause gates, reward persistence and failure return flow are integration responsibilities.

## Three finite objectives

All modes have exactly three phases. Enemy plans are reproducible from mode, tier, seed and phase. Tier input clamps to 1–100.

| Mode | Phase objective | Planned enemies | Simultaneous cap | Shared combat deadline |
|---|---|---|---|---|
| Hold point | Defeat phase enemies and accumulate 12 / 15 / 18 seconds inside an uncontested point | 6–13 per phase, scaled by phase/tier | 8 | 210–240 seconds |
| Timed breakthrough | Defeat all three planned waves | 7–14 per phase | 8 | 120–150 seconds |
| Boss gauntlet | Defeat each boss and its limited adds | One boss and 0–3 adds | 4, including only one boss | 210 seconds |

Hold progress requires `playerInsidePoint == true` and `pressureEnemies == 0`. Leaving the point or having a nearby enemy pauses capture progress, while the combat deadline continues. Clearing enemies alone is insufficient. Filling capture progress alone is also insufficient. The host supplies the point radius and pressure radius from the actual arena.

Boss index is always zero. The three plans carry HeavyStrike, RangedVolley and RelentlessCharge pattern hints; the host must actually apply these hints to enemy behaviour. A hint alone is not a distinct implemented boss encounter.

Deadlines, populations and rewards are design parameters, not measured completion times or verified difficulty. They need playtesting on the integrated arena layouts.

## Host API sequence

1. On entry, construct a fresh `ExpeditionModeState(mode, tier, seed)` and a host-owned unique durable reward run ID. Ordinary dungeon uses no mode state.
2. While gameplay is eligible to run, call `TryBeginPhase(out plan)` once. It emits the phase plan only once.
3. Queue planned enemy indices. Reserve each active slot through `TryRegisterSpawn(plan, index)` before creating that enemy. Never exceed `AvailableSpawnSlots` or create unregistered enemies. If creation fails before activation, call `CancelSpawnRegistration`; if no safe position can be resolved, explicitly fail with SpawnBlocked instead of pretending that enemy died.
4. Retain the exact plan reference and index with each spawned enemy. Call `RecordDefeat(plan, index)` on its real defeat. Old-run plans, old-phase plans, unspawned indices and duplicate callbacks are rejected.
5. Call `Advance(delta, combatActive, playerInsidePoint, pressureEnemies, playerAlive)`. `combatActive` must be false for manual pause, menus, background suspension, blessing choices, targeting/modal blockers as appropriate, and any other stopped combat state. Never use wall-clock or unscaled elapsed time as a substitute.
6. The state advances to AwaitingSpawn for the next phase, Won after phase three, or Failed with an explicit reason. Time expiration fails independently of player death. The host should return a timed-out player to camp without adding the ordinary death gold penalty.
7. Dispose on abandonment, exit or replacement. `Retry(seed)` disposes the old state and returns a fresh instance. Disposal prevents old enemy and reward callbacks from taking effect.

The clock runs only during an active, eligible combat phase. It does not run while waiting for phase creation. A deadline reached exactly at an Advance boundary is failure; progress is not granted beyond that boundary. The host must process its clock consistently relative to defeat events.

## Exactly-once reward transaction

`Reward` is a proposal with Gold, Experience and Materials. It is not a grant. Current budgets:

- Hold point: 110 + 20 × tier gold; 90 + 20 × tier experience; 1 base fragment + tier bonus
- Breakthrough: 100 + 25 × tier gold; 100 + 20 × tier experience; 2 base fragments + tier bonus
- Gauntlet: 180 + 40 × tier gold; 140 + 30 × tier experience; 3 base fragments + tier bonus

The shared `TierRewardBand` adds 0 / 1 / 2 / 3 / 4 fragments at tiers 1–4 / 5–9 / 10–19 / 20–39 / 40–100. For example, a tier-10 gauntlet grants 3 + 2 = 5 fragments. These are successful-clear materials, not per-phase grants. The entry screen previews the total. See [progression and ascension](Progression-Growth.md).

After Won, `TryReserveReward(out ticket)` allows one pending attempt. The host atomically grants and persists the proposal with its durable run-ID receipt. Call `CompleteReward(ticket, true)` only after that succeeds. A confirmed failed transaction can use `CompleteReward(ticket, false)` and then reserve a new ticket. An uncertain persistence outcome must stay reserved until reconciled; never blindly release/retry it. Stale, duplicate, foreign and disposed tickets are rejected.

The pure state alone does not guarantee exactly-once rewards across process restart. The runtime's durable transaction/receipt is required. No reward is eligible after a timeout, death, spawn failure or abandonment.

## Tests

Standalone production-rule tests cover all three modes, every phase, deterministic seeds, low/high/out-of-range tiers, concurrent spawn bounds, failed spawn rollback, duplicate/unspawned/stale defeats, uncontested capture, each pause gate, invalid/huge deltas, strict time failure, explicit failures, fresh retries, disposal and reward reservation/commit/failure/retry.

These tests do not execute Unity spawning, boss AI, real save IO, graphics, or device performance. The integrated host must be compiled and tested separately, and actual arena pacing remains unverified until a runnable Unity session is available.
