# Combat timing and summoner contract follow-up

## Verified baseline problems

- `BossAttackPolicy.ShouldAdvance` permanently limited a post-fan boss to 8.5m until it reached that range. There was no elapsed-time or progress escape.
- Poison subtracted the entire frame from its lifetime, processed only one tick, and could process a tick after the actual lifetime ended. Burn already used clipped elapsed time and up to eight catch-up events, but an expired frame could discard any excess backlog.
- Area fields processed only one due event per frame, then destroyed themselves after expiry. Gravity reset its next tick to `age + .5`, making its pulse count depend on frame length.
- A full Pack recast refreshed/commanded the foundation wolf but left existing temporary wolves' remaining lifetime unchanged.
- Perfect dodge forced a three-second recall and supplied an eight-second command token. Current source has a 14s base wolf-contract cooldown and a 12s spirit cooldown; rank3 wolf cooldown is 12.04s. Even a proposed 14s token had no safe rank1 margin.
- Charged casts retained a point/direction, but summoner contracts used the later live aim target, and spirit placement discarded the captured point.

## Implemented rules

`BossAdvanceBudget` releases the far-attack gate after 1.5s without meaningful distance progress, or after 1.8s of approach in all cases. The fallback remains latched until a new attack/reset, so a blocked approach cannot immediately restart the same lock. A valid attack still requires its real range and visibility; it cannot shoot through a wall. Stun/windup time is not counted as active approach time.

`ScheduledTickWindow` advances a finite combat-time schedule, processes at most eight due events per frame and keeps any already-due backlog until drained. Expiry is checked after eligible endpoint ticks. Pause does not advance the schedule. Consumption, stale owner or disposal clears it. The area adapter shares the same collection policy through its existing cursor and only applies its finisher or destroys itself after the eligible ticks drain. Multiple catch-up ticks produce one set of area cosmetics in that frame.

Poison uses 0.75s intervals, three stacks maximum and current stored per-tick damage. Burn uses 0.5s intervals and `totalDamage / duration` DPS; all current burn callers use 2s or 3s durations, giving four or six ticks. Neither gains critical rolls. Refresh preserves the current cadence instead of postponing the next tick. Damage counts are stable at 60fps, 200ms and 500ms frames under a stationary target; this does not reconstruct historical target positions or expired modifiers inside a frame.

Pack recast refreshes each living temporary wolf to `max(remaining, 8 + 2 × rank)` seconds: 10/12/14 seconds. It commands them and fills only vacant capacity. Recast keeps absolute current HP, clamped to the new maximum; it does not refill HP, replace living partners or add unlimited lifetime.

Perfect dodge grants companions 50% damage reduction for three seconds without moving them, changing targets or suspending attacks. Existing AoE intake is 55%, so combined intake is 27.5%. Recall mitigation and this protection do not stack twice. A single-use empowered-contract token lasts 16 combat seconds, leaving two seconds after the actual 14s rank1 wolf cooldown. The player receives no new damage reduction from this companion rule. Epoch transfer creates a fresh command state.

Charged contracts capture an enemy reference and location at charge start. A dead, inactive, out-of-range, occluded or old-scene enemy reference is rejected on release; the captured point remains the fallback and is clipped by the shared ground-point policy. A living partner can path to the captured point when no captured enemy remains. It does not select a newer live aim target. New spirit/tree placement also respects this captured point. Charge duration now reads the same `SkillDamageBudgets.ChargeSeconds` used by the coordinator's deterministic budget model.

## Shared production budget APIs

`SummonerDamageRules` provides the impulse coefficient; thorn startup, duration, interval, damage and poison values; gravity pulse/finisher timing and coefficients. `CompanionRules` supplies rank power, form attack coefficients/cadences, lifetimes, command multipliers/durations, wolf command impact/recovery, capacity and equipment modifiers. `StatusTickRates` provides poison/burn intervals. The runtime calls these values directly.

Current reviewed spell coefficients before rank power:

| Spell | Events | Direct coefficient |
| --- | --- | --- |
| Impulse | one cone hit | 1.8 |
| Thorn rank1/2/3 | 6/8/9 ticks ×0.30 | 1.8 /2.4 /2.7, plus poison |
| Gravity | six ticks ×0.30, then2.6 finisher at2.6s | 4.4 |

## Validation and limits

The focused production suites cover normal/200ms/500ms schedules, endpoint ticks, severe-hitch backlog, pause, refresh, consume/dispose, invalid values, approach fallback, HP/lifetime preservation and command token boundaries. Source contracts check real controller hooks. These checks use explicit managed boundaries; native animation, mobile interaction, GPU timing and device behavior require separate validation.
