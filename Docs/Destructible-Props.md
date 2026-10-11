# Destructible scenery

## Placement API

`DestructiblePropFactory.Create(parent, worldPosition, kind, level, blocksPath: false, recovery: PropRecovery.Energy)` creates a Crate, Pot or Rubble prop using original shared bevelled/rounded meshes and world-owned materials. It returns null for a missing/inactive parent, invalid/unsafe position, overlapping live prop or active-scene cap of 12.

Scene/arena authors own placement. Use optional side pockets, alternate gaps or decorative edges only. Never block a mandatory bridge, entrance, objective, required enemy route or safe spawn. Do not put props in enemy collections or use them to satisfy waves. Dynamic collision is optional; nonblocking props still receive physical projectile impacts.

## Real impact paths

- Vanguard basic/skill melee cones
- Friendly basic and skill projectiles, including close blocked muzzle contact
- Real CombatArea damage ticks/finishers, not the preview rings
- Player area attacks and elemental advanced areas
- Advanced sequence area/line attacks and projectiles
- Summoner impulse/gravity/area pulses

Swept projectile contact orders scenery against enemy contact, checks cover, and consumes a projectile at a prop. Props never call enemy on-hit energy, kill, poison, loot, tutorial or wave-completion routines. A prop impact uses the existing damage packet and critical flag, without another random damage/crit roll.

Per prop and cast, repeated equal or weaker pellets/ticks are ignored. A stronger later finisher applies only the increase above the cast's previous maximum contribution. The bounded 64-cast history rejects discarded old identities rather than letting them become new hits. Breaking and recovery claiming each have one terminal transition.

## Recovery and performance

Energy props restore up to 6 energy. Optional health props restore up to 3% maximum health, capped by normal healing. Limited-healing challenges convert health props to energy so scenery cannot bypass their healing charges. Full-resource characters do not get a misleading recovery log. Recovery notices go only to the bottom-left system log.

There is no persistent currency, equipment, inventory item or XP reward to accumulate by reloading. Recovery is a temporary combat resource, claimed once from that object; it is not a saved economy receipt. New room/zone instances may contain fresh props, while normal scene entry already resets or restores the character's temporary combat resources according to existing rules.

A break emits at most four original bevelled debris pieces, or two with reduced effects. At most 32 pieces are active. They use simple short-lived transform animation without rigidbodies/colliders. Materials and meshes are shared. Debris is removed after roughly one second or with its room, and its budget is released on disable as well as destruction.

## Dynamic navigation ownership

`WorldTraversal.AddDynamicCircle/AddDynamicBox` return unique reference handles. `RemoveDynamicObstacle` removes only that exact handle, invalidates cached grids and increments route revision. A handle from before Reset cannot match a new room's obstacle. Scenery unregisters on break or OnDisable; OnDestroy repeats removal safely. Inactive old worlds do not consume the next room's prop budget while Unity's deferred destruction is pending.

Impact visibility can ignore the target prop's own navigation envelope through `HasLineOfSightIgnoringObstacle`; every other wall/prop remains blocking. Underlying river, bridge, arena boundary and permanent obstacle rules are preserved when a prop breaks.

## Reusable validation

Managed production checks cover health bounds, real partial/final hits, maximum-contribution cast deduplication, finite input checks, bounded cast history with stale rejection, one recovery claim, disposal and swept contact ordering. Separate managed tests execute the production WorldTraversal dynamic-handle, removal and pathfinding code with vector/math fixtures, including old-room handle rejection and preservation of river/bridge rules. Blink is outside that fixture.

Runtime API compilation does not execute scene placement, rendering, animations, sounds, physics or a device build. Unity/device validation must cover basic and area impact feel, debris appearance, optional shortcut reachability and room-transition resource counts.
