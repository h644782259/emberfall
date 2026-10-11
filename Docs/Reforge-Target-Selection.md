# Explicit staged reforge targets

`QuoteReforgeChoice` uses the same existing `QuoteReforge` and cumulative gold function for three choices: up to five levels, the highest level currently affordable, and the current player level. It clamps to player level/100; `ReforgeChoices` removes duplicate target levels and omits an affordable choice when no upgrade is affordable. With player50/item10/gold500 the choices are 15/230, 19/450, and 50/3240. The last choice may be inspected or tracked but its execution button is disabled. No fragments are charged, and segmented gold totals remain identical.

Desktop and mobile workshop buttons now open the same dedicated scrolling selector, keeping the original cards compact. The footer has fixed track/execute/back actions. The preview shows captured target/cost, before/after attack/defense/health and remaining gold or shortage. Preview and execution both call `ApplyReforgeStats`, extracted unchanged from the existing real reforge operation; there is no second preview formula. The preview uses an independent item clone. The selected quote stays captured if the player later levels up.

`SelectProgressionGoal` adds an explicit optional `targetLevel`. The selector passes its captured target, while the existing goal catalog explicitly passes its labeled player-level target. Tracking saves that target atomically; future character levels do not silently raise it. Existing goal identity checking, quote/save-path validation and payment transactions remain intact.

The surface reconciles panel, camp, service and character identity through the existing goal-surface Update hook. Existing Back closes the nested selector through `CloseProgressionGoalSurface`; it does not exit the workshop. A failed save preserves the captured quote and displays the error for retry. Successful execution closes the stale surface.

Validation entry: `python3 Tests/ReforgeSelectionTests.py <dotnet8>`. Actual service plus actual UI partial are replayed with managed GUI shells: checks cover all three real quotes, selection buttons, disabled unaffordable execution, preview/payment equality, exact-target tracking after levelup, save failure/retry, stale/cross-save rejection, target deduplication, Back/panel/camp/character reset. Three negative controls must compile and fail their named assertions: defaulting tracking to player level, treating affordable as full catchup, and bypassing real preview recalculation.



UI replay does not establish font fit or real touch ergonomics; those require native UI and device checks.
