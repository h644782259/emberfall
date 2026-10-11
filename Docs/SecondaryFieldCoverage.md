# Stable secondary elemental fields

`ElementalFieldVisual` previously rotated every ground-field emission point over time, then switched each entire bubble/strand on or off using a swept `.55` radius from the field center. When the center was closer than `.55` to a wall, that sweep could reject every piece even when a clear sector remained.

Ground fields now choose deterministic visible-side placements once, reserving each piece's maximum sway/jitter and width. Coverage checks use the existing center line-of-sight and a full footprint at the destination. The field origin no longer orbits over time. Poison still rises/pulses, fire still sways and lightning still branches, within those local envelopes. Lightning's top remains above its placed footprint rather than drawing a diagonal back toward the potentially obstructed center. The original on-body aura behavior is preserved.

The cached plan refreshes when the field transform or `WorldTraversal.Revision` changes. During refresh, each still-valid placed envelope retains its local position and scale: removing nearby cover or changing unrelated traversal geometry no longer teleports a safe piece back to its original preferred point. Invalid placements are replanned; previously omitted pieces may appear when space becomes available. A static field makes no repeated coverage queries merely because its animation advances. Replanning after an actual geometry/transform change can still add, remove or relocate cosmetic pieces; this change does **not** promise smooth visibility under every dynamic obstacle configuration. Fully blocked fields remain omitted. Damage and LOS rules are unchanged.

Maintained checks:

- `FilledVfxAllocationTests.py` executes the actual `ElementalFieldVisual` component with managed Unity substitutes, covering all three field elements, animation samples per element, actual strand points plus width, growing bubble extents, stable visibility, no repeated queries on static frames, revision refresh, fully blocked fallback and on-body regression.
- A mandatory negative control reinstates the former rotating `.55` sweep/toggle. The production near-wall test must reject that regression with `wall-adjacent field must keep visible secondary pieces`.

Run `Tests/FilledVfxAllocationTests.py` with the dotnet executable supplied through `DOTNET`. Its allocation coverage remains limited to its impact-placement path. Crescent, Charge and Thrust retain their prior unreserved cosmetic footprints; this secondary-field fix does not extend coverage guarantees to them.

## Safe-placement continuity regression

`Tests/ElementalFieldContinuityTests.py [dotnet-path]` executes the production component using the existing managed Unity substitutes. With animation time fixed, a half-plane wall at x=.25 first forces alternative placements. Removing that wall must retain safe rendered points. The suite covers fire, lightning and poison, including retained strand points/bubble positions and scales, unrelated revisions, zero extra static queries, transform-only invalidation/restoration, newly blocking cover and later reopening. Its mandatory negative control removes the retention branch and must fail the rendered-point retention assertion.

Tests model analytic wall coverage; they do not constitute Unity 6 engine, real physics, GPU or device acceptance. This fix deliberately adds no interpolation: a newly obstructed existing position can still move or disappear immediately.

The continuity check accepts the dotnet path as its first argument or through the `DOTNET` environment variable.
