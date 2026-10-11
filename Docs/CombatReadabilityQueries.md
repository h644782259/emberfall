# Current shatter action and failed-room evidence

## Shatter availability

`PlayerController.CanShatterNow(1)` queries the current meteor action, separately from a displayed enemy's frost status. The query and actual preparation/release share `ResolveMobileSkillAim`, `ResolveSkillGroundTarget` and `SkillTargetingReady`. During a desktop target preview, the same skill's current `TargetPoint` owns the query; another targeted skill blocks it. Mobile targeting uses the real focus/nearest selection policy without searching preferentially for frost enemies. The current impact footprint includes rank range, enemy body size/footprint and area LOS. The read-only query does not spend energy, choose an aim target, start a cooldown or emit failure feedback.

The HUD consumes this query independently of whether its displayed target has frost, or whether a displayed target exists at all. Frost remains a status when the action is unavailable. A positive query describes current geometry and state only: enemies and frost duration can change before meteor impact. Charging reports no current shatter action, not a promise about future release.

`Tests/ShatterAvailabilityTests.py <dotnet>` compiles the real Player query/resolver method bodies, production mobile targeting policy, production skill description/balance, and the actual HUD opportunity method and presentation. Managed vector/LOS/host substitutes model analytic walls. The checks cover 8/10/13 metres, a nearer unmarked enemy, focus priority, walls, rank/body size, resource and input gates, desktop chosen point and active targeting, and HUD status/action separation. Mandatory negative controls restore the old frost-plus-ready shortcut and the old actual HUD ready-only wiring; both must fail. Release/preparation callsite checks confirm shared methods. This does not execute the Unity engine, physics or future charged/animated impact.

## Room failures and evidence

`RoomChainState.Failure` preserves Death, Timeout, Abandoned or GenerationOrPathFailure once terminal. Existing corridor rooms still have no deadline; this change does not add one. Arena timeout reasons remain compatible with existing names. Death uses the death callsite; spawn/reachability failures use the generation/path cause. A successful voluntary return captures Abandoned only after the existing save-before-leaving gate succeeds, before the old world is destroyed.

The actual session summary now carries the cause, room/stage, observed objective progress, actual damage and actual healing. Death can display the last real hit; timeout, voluntary exit and generation failure do not present an earlier hit as a lethal event. Path failures advise reentry, voluntary exits do not invent combat advice, and no-healing advice requires real damage and no recorded effective healing. Player.Heal records its clamped actual delta rather than the requested amount. These diagnostics neither alter rewards nor create a death penalty for other failures.

`Tests/RoomFailureEvidenceTests.py <dotnet>` executes actual session summary/damage/healing method bodies with the real room state and recap presentation: Checks cover evidence, state, summary members and recap layout; a negative control restores the old failed→Abandoned summary mapping. Death/path/abandon integration sites are checked as source contracts; full scene travel is not simulated.

Crystal-side-event membership is bound to current context, player and combat epoch. Both registered live enemies expose the remaining 2/1 query; defeated, inactive, stale or abandoned members do not. The independent marker component attaches only after both spawns register successfully. Markers do not depend on the destroyed crystal object.

Both Python checks accept the dotnet executable as argv[1] or DOTNET. No Unity/device/GPU/frame-time or gameplay footage acceptance is claimed.
