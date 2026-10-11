# Companion pose attachment and authoritative feedback

C06 keeps attack timing unchanged. `SummonedCompanion.Update` still decrements the existing cooldown, performs its existing range/path-gated release and resets the original attack interval. The read-only `AttackPreparation` then exposes the last .22 seconds of an existing cooldown only while a valid target is in actual attack range, reachable, not in recall and not still displaying contact recovery. Dead/distant/blocked targets cancel the preview; it is not a guarantee of a future hit and never delays or creates an attack. `SetCompanionAttackPreparation` changes only the model pose.

The companion hardware frame follows the body's translation and rotation around its authored pivot, with scale fixed at one. Contract chest/shoulder/crown additions, the spirit crown and treant crown use it. It excludes spirit soft-body squash. The spirit crown has a bounded .025 local hover offset and the live recall insignia a .04 offset; pose updates do not create parts/materials. Expired/Defeated/Replaced detached exits, active companion limits, free recall and free focus semantics are retained.

C03 offers `SummonedCompanion.EmpoweredHitFeedback(owner, out int sequence, out int hits, out float age)`. This is a read-only two-second event window; `hits` is exactly one actual health-loss event, not an aggregated cast target count. Sequence increments only through `RecordEmpoweredHit(enemy, actualHealthLoss, empoweredAtRelease)` with finite positive actual health loss and a live owner/epoch. Free focus and recall do not mint events. Existing `CommandOpportunityRemaining` remains the source of opportunity expiry. `EmpoweredAttackActive` supplies the release snapshot; the CombatEffects owner must capture it at projectile launch and pass the captured bool with the actual health difference, never re-read the later command state.

## Reusable validation

- `Tests/CompanionIntentProductionTests.py` exercises the production order, command, preparation and health-loss feedback methods. Compiled negative controls reject blocked-target preparation and feedback without health loss. Source wiring checks bind preparation to Update after its real release.
- `CompanionAppearanceTests` exercises the production builder, attachment and retirement logic, pose rotation/pivot rules and bounded part counts. Its compiled negative control rejects root-fixed attachments.
- `Tests/ContractSnapshotProductionTests.py` checks release snapshots. `Tests/BlessingDamageProductionTests.py` checks fixed-roll damage budgets and actual partner paths with a negative control.

The existing companion-intent-production and companion-appearance registrations own these checks. Managed fixtures do not validate rendered meshes, Unity animation scheduling or device input; runtime API compilation is a separate check.
