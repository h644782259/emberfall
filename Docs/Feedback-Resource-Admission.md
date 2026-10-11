# Combat text admission and log views

Both session text callers previously checked a count of 48 even though the text
renderer accepts only 20 mobile or 36 desktop numbers. At the actual limit, an
ordinary hit therefore created a GameObject and component that initialization
immediately rejected and destroyed.

The shared `FloatingNumber.Spawn` factory now evaluates global capacity and local
lanes before creating anything. One read-only scan chooses the oldest ordinary
replacement for an incoming critical, excludes it from prospective local
occupancy, and keeps the existing four ordinary / six critical local thresholds.
Only a fully accepted spawn retires the replacement. In particular, a critical
rejected because its origin is crowded cannot erase unrelated ordinary text.
The selected replacement, lane and camera stay local to the immediate call; the
factory does not repeat the scan or retain a plan across frames. Public
`Initialize` remains supported and uses the same policy. Registration, retirement,
count decrement and destruction semantics remain centralized in their existing
paths.

`SystemMessages` now caches its read-only collection wrapper once per session.
Readers keep the same live view as messages append or the oldest entry is evicted
at the existing 32-entry limit. Collection mutation remains disallowed.

`FeedbackAllocationSourceTests.py` checks the creation/admission order, shared
callers, local limits, replacement semantics and cached wrapper. The isolated
`FeedbackValidation` fixture prepares actual engine assertions for both display
limits, critical-only saturation, rejected-spawn object counts, local lane reuse,
non-destructive failed replacement and the read-only live log. It requires a quiet
scene and restores its mobile simulation and log changes. The dense-rejection
case starts with six critical entries together, then adds ordinary entries far
away; replacing a distant ordinary entry cannot free a local lane. This is
separate from the mixed ordinary/critical lane-limit case. These are prepared
checks, not executed Unity memory measurements or PlayMode results. Source/API
validation cannot establish frame-time or native allocation performance.

## Measured layout and mechanism reserve

Damage retains its 20 mobile / 36 desktop budget and four ordinary / six critical
local admission thresholds. Explicit `SpawnMechanismText` callers (interrupts,
class procs and boss phase messages) instead share four reserved slots. Damage
cannot replace a mechanism caption; a mechanism may replace a damage label only
if doing so actually yields a valid rectangle. Numerical healing and received
damage stay in the ordinary budget. Total active text is bounded at 24 / 40.

Before allocating a text object, font glyph extents, accessibility size, display
density and the maximum critical pop determine its padded rectangle. Twelve
candidate positions use that measured height and width; a crowded or clipped
caption is rejected before GameObject creation. LateUpdate reprojects all labels
after camera movement and checks actual renderer bounds. Labels that no longer
fit retire immediately. Disabling a label returns both applicable counters.
Captions are limited to 24 characters (plus a critical suffix); longer detail
belongs in the HUD or log. This bounds layout work, not the Unity font engine's
internal native allocations.

The mobile HUD keeps its existing touch areas. HP numbers, potion inventory or
limited-healing charge count, blink seconds and skill energy/charge states appear
inside those areas. Actual skill/traversal rejection points provide short local
feedback; it expires after 1.1 seconds and is suppressed on pause/death or epoch
change. Three persistent world marks distinguish automatic aim (open cyan ring),
companion focus (green diamond), and fixed charge point (closed gold ring).
They read runtime targeting and do not change targeting or spending rules.

Run the new pure checks with:

```bash
python3 Tests/Run-CombatFeedbackTests.py --dotnet /path/to/dotnet
python3 Tests/FeedbackAllocationSourceTests.py
```

`MobileCombatFeedbackTests` depends on `MobileCombatPresentation.cs`;
`CombatTextLayoutTests` depends on `CombatTextLayout.cs`. The prepared Unity
`FeedbackValidation` fixture retains full-cap, replacement and preallocation
rejection checks and adds independent-pool saturation. It uses a dimension-only
camera render target for layout coordinates (never Create/Render) and restores
the previous target. These engine checks still require Unity execution. No new
screenshot, rendered text readability, frame-time or real-device result is claimed.

## Single opportunity and free companion orders

The HUD now uses at most one class opportunity line. It reads the living current
scene target, counter timer or live companion roster; dead, paused and changed
combat epochs suppress prior state. Frost/burning, three poison stacks and
vulnerability are actual status flags. The frost message claims shatter readiness
only when the non-burn route's meteor is learned and presently usable. The Burn route instead offers "灼烧 · 陨星续燃" only for a currently burning
target when meteor is learned and its actual cooldown, energy, airborne and
charge/frame-lock gates are clear. This describes refreshing the existing burn,
not an extra damage bonus; unrelated frost does not mask the Burn route's message.
The current charging skill is explicitly marked, while unlocked ready skills
retain a small ready indicator.

Summoners have two separate free buttons on touch and desktop: focus and recall.
The touch areas are 48 logical units wide in the gap left of the two skill rows;
these do not consume any of the ten skill slots. Physical touch/mouse ownership
submits each command once; the mobile IMGUI surface does not submit a duplicate.
They call only `SetFreeFocus` / `FreeRecall`, never a contract skill. An absent
or out-of-range target gets a short local result. The existing free-order APIs
own all gameplay restrictions, and remain independent of contract cooldowns.

The standalone feedback runner additionally exercises opportunity formatting and
all control geometry, including 568×320. `CombatOpportunitySourceTests.py` checks
command dispatch/lifecycle wiring. This is not a touch-device or Unity visual test.
