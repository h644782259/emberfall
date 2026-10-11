# Integrated player progression journey

This adds regression coverage across existing services; it introduces no gameplay or production changes. The test starts from a real isolated save, completes chapters through the existing GameSession host, claims the shared first core, grows that same item and skill build, reloads, and returns to camp through storage failures and retries.

## Actual sequence

`Tests/IntegratedJourneyTests.py` compiles the actual ProgressionService partials, ChapterCombatRun, chapter host, and extracted production save/return/load methods. Its two fixture files run all four classes with fresh and legacy-shaped level-one saves: eight journeys and 72 chapter attempts. The legacy case edits the raw saved envelope to contain three unlearned skill ranks before calling LoadSlot; it does not call the current Save method to migrate the input first.

No test directly grants levels, currency, chapter unlock masks or core entitlement. Normal tier-one chapters provide the progression. Controlled player/enemy positions satisfy objective boundaries; this does not demonstrate a player can safely evade enemies in actual gameplay.

| Stage | Observed level / XP | Gold | Fragments | Relevant result |
|---|---:|---:|---:|---|
| Fresh or migrated start | 1 / 0 | 60 | 0 | Exactly one first active skill, no free point |
| Forest seals then exit, zero kills | 3 / 54 | 60 | 2 | Skipped enemies do not donate death XP |
| Redrock designated target then exit | 5 / 32 | 71 | 4 | One registered death; no early core |
| Star boss and two guards | 6 / 76 | 202 | 7 | Shared first-core entitlement becomes durable |
| Core claim and one slot upgrade | 6 / 76 | 142 weapon / 157 relic | 7 | One locked Epic level-six item, stable ID |
| Five zero-kill Forest repeats | 11 / 346 | Unchanged | 12 | Real rank-two skill gate and partial-reforge window |
| Affordable reforge, reset, old preset | 11 / 346 | 34 weapon / 9 relic | 12 | Weapon reaches level nine; relic reaches ten |
| Reload and another Forest return | 12 / 358 | Unchanged | 13 | Grown item, slot rank and shared claim remain intact |

The first class-appropriate mechanic is used (ReturningBlade, FrostEcho, VenomSpread or TwinSummonResonance). The existing managed Random boundary returns 8 for the ordinary gold roll and .9 for the loot-chance input. These table values describe that reproducible fixture, not an economy average or actual player experience.

## Cross-feature invariants

At every stage, free points plus skill ranks plus mastery ranks equal the actual level budget. Inventory IDs remain unique; first-core claim flags remain consumed through equip, permanent slot training, reforge, reset, preset and reload. Every transaction re-reads the item by ID because successful candidate commits may replace the profile and item objects.

The preset is saved before reforge. Applying it after skill reset must restore the investment without restoring the old item level, undoing slot training, duplicating an item, or changing currency. The affordable quote must be strictly below character level and maximal for the earned gold; preview cannot mutate the profile or files. Retrying a stale committed quote cannot charge again. Only advanced skill ranks are refunded: first ranks and hotbar/equipment identities remain. Mastery stays zero in this bounded level-one-to-twelve journey; it does not prove nonzero mastery refund behavior.

## Real storage failures and retries

All files are beneath unique temporary save roots. Creating an empty directory at the exact `.tmp` path exercises the production filesystem refusal, not a simulated boolean or a disk-full/File.Replace interruption. Core claim, equip, slot upgrade, reforge, skill reset and preset application fail without publishing changed profile, documents or notification events; removing the owned blocker permits the same operation to retry.

For the final Star kill, earned gold/XP deliberately remain live when saving fails. Completion fragments, unlocks and claim entitlement must not publish yet. The same pending chapter receipt retries and persists both the live earnings and completion exactly once. Replaying the enemy callback or durable receipt never pays twice.

UI return failures preserve the chapter receipt, result, world, combat epoch and frozen clock. Success then clears the chapter state and retires the old world once. After the build is complete, corrupting both saved documents makes the real same-slot load staging fail while the current character and paused world remain intact. Restoring those exact documents allows the same load request to retry; a freshly staged service retains the entire build. Subsequent chapter reward and failed-return retry preserve the grown core. A later legacy-mode reward uses the same already-consumed first-core entitlement.

## Explicit boundaries

System.IO and the production persistence/services execute. JsonUtility is the existing System.Text.Json IncludeFields shim, not Unity serialization. Player movement and AI positions, input, scene objects, rendering and timing are managed boundaries. Destroy deactivates a fake object; real Unity deferred destruction and MonoBehaviour lifecycle callbacks are not exercised.

The inherited host stubs enemy loot delivery, world-loot preservation, side-event settlement, input suspension, combat effect cleanup and other modes' defeat callbacks. Thus inventory assertions cover the controlled first-core journey with no delivered random loot, not a complete combat-loot lifecycle. Full Unity Update, damage, VFX, player feel, mobile interaction and device performance remain unverified. No artwork or PR25 sampling behavior is changed by this test round.

## Reproduce

```sh
python3 Tests/IntegratedJourneyTests.py /path/to/dotnet --output /tmp/emberfall-journey-evidence
```

Omit `--output` for temporary evidence outside the repository. Exact generated host sources, hashes and raw output are retained per run; production files are read-only. The test is registered as `integrated-player-journey` in `Tools/cloud-validation.py` and runs with the rest of the suite. Six compiled controls cover skipped XP redistribution, missing legacy migration, omitted reforge debit, forgotten first ranks, preset rollback of permanent slot training, and world discard before a failed load has staged. Compiled negative controls must build successfully before failing their named journey invariant; compilation errors are not accepted as negative evidence.
