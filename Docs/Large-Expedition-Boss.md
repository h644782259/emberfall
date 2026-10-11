# Large expedition: Star-Ring Archon

## Integration

Call `LargeExpeditionBoss.Configure(enemy, DungeonTier, seed)` only after the final-room Guardian boss is initialized. Repeated configuration returns the same component. Small-trial boss pattern 0 and the three gauntlet variants are unchanged. Existing tier/level health and damage remain; the encounter is differentiated through geometry and counterplay, not additional HP inflation.

The original astrolabe rig has four articulated radial limbs, opening core-armour petals, two counter-rotating authored torus meshes and a suspended luminous engine. It replaces the small giant rather than scaling it. Navigation radius is 1.25m; projectile radius is 1.3m, and melee/AoE reach gains a large-boss-only footprint allowance. Materials remain in CombatModel's owned palette, including recoil/death fading. One 240-vertex/480-triangle torus mesh is shared by both rings in each boss.

## Actual mechanic

At 70% and 35% health, once each, the boss stops ordinary attack scheduling and creates up to three destructible power anchors. A 2.2-second floor-path warning fills toward its end and shows a clockwise sweep arrow. Cyan means a qualifying control skill can interrupt through the existing per-cast and 5-second boss recovery policy. Amber/red means that policy is unavailable or the beam is already active. The active sweep is not interruptible by ordinary stun.

The single beam rotates 24 degrees/second for at most 14 seconds. Its half-width is 0.55m, outer reach 9m, with a 1.5m inner gap. Damage uses the existing boss attack budget multiplied by 0.55, checked at most every 0.65 seconds against the actual position and ground line of sight. The first damage opportunity is delayed after the warning. No catch-up burst after a long frame. Walls clip both shown path and damage.

Destroying all available anchors or successfully interrupting windup instantly cuts the beam and opens the core for six seconds, during which incoming damage is multiplied by 1.35. The boss is always damageable and can be killed without completing the mechanic. Ignoring anchors eventually ends the phase with two seconds of recovery, without a bonus. Four seconds of ordinary combat separate consecutive threshold mechanics.

Anchors are 2.6m from the core with 0.55m target radius, inside the reach of existing boss-targeted 2.5m AoEs. This provides one-tap mobile counterplay without precision ground selection. They share normal destructible cast deduplication and a 12-prop active-scene cap; eight scene props plus three anchors fit. Placement requires safe ground, a clear path to the core and player clearance. Missing safe placements fail open. Anchors grant no recovery, gold, XP or items and never join enemy/wave counts. Their roots deactivate immediately before disposal or the next phase.

## Lifecycle and verification

EnemyController explicitly owns the component's tick, interruption and death cleanup. No coroutines or delayed damage are scheduled. Menus, background pause and blessing choices freeze the mechanic; scene disable, player death, boss death or terminal mode state dispose it. Reduced effects simplify the beam core while retaining essential floor boundaries.

Pure checks cover threshold, pause, anchor, interruption, timer and disposal rules; source contracts check wiring. Runtime API compilation is a separate check. Existing non-large enemy footprint values are preserved.

Unity PlayMode, rendered appearance, mobile targeting feel, device performance and boss combat tuning require runtime validation. The timing and tuning above are design targets, not measured playtest outcomes.
