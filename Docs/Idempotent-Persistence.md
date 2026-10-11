# Idempotent saves and checked scene transitions

The public save API is unchanged. A successful unchanged save can now be a read-only operation. The service first applies the deletion, attached-slot, create-only collision, recoverable temporary document, validation and 4 MiB serialized-size guards. It then reads the actual primary through a bounded file handle and compares it with the complete current validated UTF-8 document. It skips replacement only if that document matches exactly and the backup also loads as a usable save. No dirty bit, last-write timestamp or cached file length determines this decision.

This preserves the pre-reward backup when a dungeon/mode reward transaction is immediately followed by a manual save, background save or leave operation. A direct change to `Profile`, including a change made by a reward callback, still produces a different document and commits normally. An unchanged save preserves both files' bytes and timestamps. Repeated confirmations and receipts do not create extra backup generations.

## Repair and refusal boundaries

- A valid primary with a missing or corrupt backup goes through the existing atomic replacement, restoring its previous primary as the backup.
- A usable backup with a missing or corrupt primary permits primary repair while retaining the backup. Corrupt primary bytes never replace the usable backup.
- If neither existing document is trustworthy, saving fails before a temporary write. Both damaged originals remain available for manual recovery. The error explicitly states that current progress has not been saved. The service does not publish an uncommitted candidate into a replacement backup.
- A genuinely new unattached legacy save uses the same create-only path as a new character. A deleted or moved attached slot is never treated as new creation.
- A recoverable `.tmp`, a blocking temporary directory, or a deletion marker still refuses saving even when primary contents otherwise match. Existing recovery data is not cleaned up to enable a save.

Bounded reads allocate at most 4 MiB from the length of the opened handle, consume only that many bytes plus one growth check, and reject truncation, growth, invalid encoding or unsupported save documents. Loading retains the previous BOM-detected UTF-8/UTF-16/UTF-32 import compatibility. Exact write comparison still uses the canonical BOM-free UTF-8 document, so noncanonical imports are safely normalized on the next write.

## Scene adapter order

`GameSession.ChangeZone` completes `SaveBeforeLeaving` before stopping waves, destroying objects, clearing enemies, resetting run state, moving/refilling the player, or resetting entry cooldowns. Failure returns with the old scene intact. `ConfirmDungeonSelection` restores the previous `ChallengeRun`, reopens its selector and keeps the error visible. There is no unchecked save after construction.

The existing `loadingSaveSnapshot` guard skips this preflight only while constructing a staged loaded character or rebuilding a hub whose destination transaction already committed. Load discards old transients before publishing the staged service; hub travel preserves ground loot before committing its destination. Both callers reset the guard in `finally`. This change does not add world-construction exception recovery.

Room-door travel now runs the same complete checked save before advancing `RoomChainRun` or tearing down its scene; it still retains skill cooldowns. Starting a new character and the compatibility snapshot API settle the old character's pending receipts before switching the autosave target. New-character publication clears the old equipment fingerprint first, preventing an old character's equipment-change tutorial callback from writing into the new character.

Saving rewards and ground loot can commit those independent transactions before a later save fails; they remain durable and receipts prevent duplicates. The preflight guarantee is that scene teardown, run reset and entry cooldown reset do not begin on failure. It does not roll back rewards that already committed.

## Verification

### Coupled earned kill rewards

`OnEnemyKilled` admits the current enemy once, then calls `GrantEnemyKillReward` for kills, gold, experience and any level/skill-point gains. The service updates the existing live profile and makes one save attempt before one `Changed` notification and ascending `LeveledUp` notifications. Negative reward arguments are rejected before mutation. Gold and kills saturate safely; maximum-level experience cannot mint additional points.

Unlike a purchase or a pending completion receipt, a failed earned-reward save does not revoke progress already earned in combat. The exact live profile and selected destination remain, the failure stays in `LastError`, and recovery retries `Save()` rather than granting the kill again. Existing deleted/moved-slot guards still prevent resurrecting a destination or writing another character.

The final session save remains necessary for real changes made by reward callbacks. Without such changes it is the existing read-only exact-document comparison, preserving the backup from before the entire kill reward. Item rolling still uses the post-reward level. Actual item acquisition, optional side-event rewards and completion receipts retain their independent transactions. Consequently the ordinary below-cap kill reward removes one content-changing replacement and one duplicate stats refresh; it does not claim every possible kill produces only one write or establish a measured frame-rate improvement.

`EnemyKillRewardTests.Run(directory)` covers exact rewards, all classes, no/single/multiple level gains, ordered final-state events, saturation, invalid arguments, failed-save live progress, one successful save-only recovery, selected/deleted/moved destinations, post-level item rolls, independent collection and callback mutations in isolated fake-save roots. `EnemyKillRewardSourceTests.py` checks the current-enemy admission, service call and final-save ordering. These are standalone/service and source checks, not Unity execution.

- `SaveIdempotenceTests.Run(directory)` exercises the actual service beneath unique fake-save directories: unchanged bytes/timestamps, direct mutations, same-size/same-time external edits, both repair directions, both-damaged refusal, creation collisions, temporary/deletion refusal, reward receipts, callback mutation and bounded/encoded documents.
- It also covers linked primary/backup/temporary/deletion paths and dangling links. A regression originally reproduced a malformed `.tmp` link being followed by `FileMode.Create`; the writer now rejects the link before any temporary write.
- `PersistenceTransitionSourceTests.py` checks the actual adapter ordering, rollback, guarded load/hub paths and bounded-read implementation. These are source contracts, not engine execution.
- `PersistenceTransitionValidation.Validate(game, check, log)` is a prepared synchronous Unity fixture for the isolated `RuntimeValidation` runner's first living camp, after inventory fixtures and enemy freezing. It verifies repeated blocked portal entry, preserved world/enemies/run/cooldown/energy, blocked camp/room transitions, new-character/snapshot selection and leave, and exact primary/backup preservation. It rejects non-isolated paths, pending loot/rewards and pre-existing temporary paths, and restores its temporary profile/runtime/position/UI state. Unity execution is required to validate the prepared fixture; API compilation is a separate check.

No fixture reads player save directories or performs real clipboard, user-data or external-service operations.

## World receipt retirement and rejected wilderness drops

Successful same-character zone/room teardown now retires its collection-ID sets
only after the checked preflight, empty pending ground queue, deactivated old
producers and invalidated old combat epoch. Rejected wilderness collection keeps
the exact drop in the session queue with bounded automatic retry frequency and
replacement-spawn backpressure. See [live builds and world-loot ownership](Build-And-Loot-Transitions.md)
for the boundary order, limits, executable tests and prepared-only engine checks.
