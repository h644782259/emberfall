# Live builds and world-loot ownership

## Companion investment follows a committed live build

The previous permanent Spirit kept the rank of its original contract. A camp
refund, combined reset, or lower-rank preset could therefore leave an awakened
Spirit dealing rank-three damage after the hero had paid for only rank one.

`PlayerController.RefreshStats` now reconciles living companions synchronously.
This is reached by a successfully committed profile change even while menus pause
normal Update ticks. Route and capacity changes happen first: a Pack Spirit that
has just become Bonded becomes permanent before immediate travel, and a two-pet
mechanic enforces its cap without waiting for the panel to close. The starter wolf
uses learned skill 2; a permanent Spirit uses learned skill 4. Timed casts retain
their cast rank. A permanent Spirit converted to Pack is reconciled before its
finite lifetime is assigned, including when the same preset lowers its rank.

A power refresh preserves absolute current HP, clamping only to a smaller maximum.
Rank upgrades, increased hero health, equipment toggles and repeated preset
application do not heal the companion. Its current command keeps its remaining
time and paid empowerment, but its rank contribution updates. Refreshes do not
reset attack recovery, hero skill cooldowns or energy, consume or grant command
opportunities, or recast a contract. Explicit healing and fresh summoning still
work through their existing paths. A normal encounter transfer retains the
existing policy of clearing old commands/opportunities and applying a minimum
0.35-second attack recovery.

Transfer reconciles the current build before invalidating its old epoch and again
refreshes the surviving permanent body's power on arrival. This defensive path
also covers a direct profile mutation that did not emit an event. Route changes
use the existing oldest-first eviction rules and never resurrect defeated pets.

## Rejected wilderness equipment stays owned

The wilderness enemy callback previously used `CreateLoot`, whose compatibility
return value identifies a roll without proving that its collection saved. It then
showed success even when a full protected mailbox or failed write rejected the
item. The callback now rolls once, explicitly checks `CollectLoot`, and retains
the exact failed item in the existing pending-world queue and a visible pickup.
Only successful collection emits acquisition feedback. Dungeon drops retain their
normal visible-landing path.

Pending pickups now work in the wilderness. Automatic collection verifies both
the exact registered pickup instance and its membership in the current active
world. A retired or foreign pickup cannot collect an entry merely by reusing its
string ID. Failed automatic attempts wait one second of gameplay time before
retrying; pause still prevents attempts. Exit preflight also preserves/retries
these items through the existing bag, pending mailbox and recovery mailbox rules.

The first pending wilderness item prevents replacement wilderness enemies from
spawning. Already living enemies retain their rewards, so no valuable equipment
is silently dropped or sold to impose a cap. With the existing population ceiling,
up to 22 already living wilderness enemies can still produce pending drops before
that world's producers are exhausted. Spawning resumes when the ground queue is
empty. A full recovery mailbox still blocks departure rather than deleting data.

## Receipt lifetime ends only after producers retire

Collected/autosold item-ID receipts previously accumulated across every zone and
room of one character session. For same-character zone and room transitions, both
the service and session receipt sets now share a guarded retirement boundary:

1. Complete the checked save/loot preflight, or use the existing already-committed
   staged-load/hub path
2. Stop the old wave coroutine where applicable, deactivate all old enemies and
   transient objects, clear their registries, and deactivate the old world
3. Invalidate the old player combat epoch without moving against old terrain or
   resetting skill cooldowns
4. Require an empty pending ground queue and all retired-producer/epoch guards
5. Clear both sets, then construct the next world and perform the normal teleport
   and spawn sequence

A failed preflight, ordinary save, manual sale or wave change never releases these
receipts. The service helper is idempotent and does not save or emit a profile
change. Durable inventory and mailbox identities continue to prevent duplicate
acquisition after world receipts are retired. Existing explicit character
replacement/load behavior remains separate from this same-character boundary.

This releases retained GUID references across successful world transitions; it
does not claim a fixed receipt bound during an indefinitely farmed single world,
nor measured heap, frame-time or native-memory improvements. Hash-set capacity
may be reused rather than shrunk.

## Verification and limits

- `CompanionRulesTests`: rank selection,
  command scaling and repeated no-heal rank/gear changes
- `WorldLootReceiptTests`: checks against the actual progression
  service and isolated fake-save roots, covering failed-save/capacity retry of the
  same item, all incomplete retirement guards, autosale/manual-sale duplicate
  protection, durable bag/recovery identity, and repeated world retirements
- `BuildTransitionSourceTests.py`: companion adapter/source contracts
- `WorldLootReceiptSourceTests.py`: pickup/transition/source contracts
- Prepared `CompanionBuildValidation` invokes actual paused refund, reset, preset,
  skill purchase, mechanic-equipment and transfer APIs. It also checks failed
  commit rollback, health, attack recovery, skill recovery and command tokens
- Prepared ground-loot checks cover real wilderness pickup updates and retry
  backoff, protected overflow, producer backpressure, successful room retirement,
  late old-enemy callbacks, and the existing camp/title paths. Prepared persistence
  checks retain both receipt sets through failed preflight attempts
- Related regression coverage includes progression, save idempotence, build
  presets, explicit-action persistence, combat pacing and source contracts
- API compilation does not execute the engine or establish Unity
  PlayMode/render/device behavior

No test reads a player's save directory or uses an external service.
