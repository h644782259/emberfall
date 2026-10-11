# Chapter experience accounting

Each attempt freezes the character's actual entry level, including level 1. Normal enemies use `22 + 2 * entryLevel`; the boss uses `100 + 12 * entryLevel`. Each registered death pays the integer floor of 30% of its standard share. Completion pays the full planned budget minus **every registered death share**, including enemies the player bypassed. Integer remainders therefore stay in the completion award. Gold and drop formulas retain their existing behavior.

Forest and Redrock register six enemies in each of two rooms; Star registers its boss and two guards in its single room. Completion rejects incomplete registration. A death must pass the current host run/room/epoch identity before either chapter experience or legacy gold/drop rewards are granted. Duplicate or stale callbacks pay nothing. Failure retains already earned death experience and does not redistribute skipped shares.

The completion transaction atomically publishes experience, levels, materials, node/difficulty progression and the idempotent receipt. A failed save leaves the result pending for the existing guarded leave/retry flow. Only completing Star Platform finishes the chapter and enables the existing shared, one-time first-clear core claim; reload preserves eligibility, and an already claimed core cannot be claimed again.

A level-1 Forest entry fixes the full budget at 288. With no registered enemies killed, completion pays 204; one kill pays 7 plus the same 204. The old live-level full-kill path could award 320 because leveling changed later enemy awards. This is an explicit accounting change, not a measured pacing or enjoyment claim.

`ChapterExperienceTests.py` executes the real progression services across levels 1–100 and all three nodes, persistence/failure/replay cases and compiled negative controls for skipped-share redistribution, missing shared-core eligibility and incorrect level-2 clamping. Real host admission and save/return handling are also exercised by `ChapterHostProductionTests.py` and `ChapterReturnTimeScaleTests.py`.
