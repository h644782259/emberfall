# Weapon visual connections

This follow-up consumes `CombatModel.TryGetWeaponVisualAnchor` and `WeaponSwingSide` from the weapon-structure change. It follows A04's primary-volume change. It does not replace a safe projectile origin with an animated weapon coordinate.

`CombatProjectile` retains its simulation transform, scaling, velocity, radius, target and collision processing. Its existing mesh and trail now live on a renderer child. For player shots, that child initially uses BowArrowRest or StaffCore when the anchor is finite, nearby and on a clear side of cover. It samples the completed release pose before the first render, then detaches and smoothly rejoins the unchanged projectile trajectory in 0.09 seconds. Invalid/covered anchors fall back to the simulation origin. Companion and hostile shots retain their original visual origin. The trail starts after the visual origin is assigned, avoiding a line from the temporary creation position.

Sword slashes also create a bounded, short-lived filled ribbon using both live SwordRoot/SwordTip endpoints. Its trailing edge and the broad crescent share the model's alternating swing side. Pause freezes its age; owner/epoch/result changes retire it. This is an additional visual mesh, not a hitbox or target query.

Maintained checks:

- Production visual-component checks use explicit managed transform and anchor substitutes to check actual child placement, first-frame pose recapture, timed convergence, blocked-anchor fallback, unchanged simulation transforms under nonuniform scale/rotation, both sword endpoints, left/right mirroring, alpha and epoch retirement.
- A mandatory negative control injects a write to the simulation root and must fail the root-invariance assertion.
- Source checks cover wiring, basic timing, combat-review and weapon-structure contracts.

Run `DOTNET=/path/to/dotnet python3 Tests/FilledVfxAllocationTests.py` for both production-component suites and both mutations. `python3 Tests/WeaponVisualLinksSourceTests.py` provides separate wiring checks.

The anchor provider is an explicit test substitute; these tests do not establish the appearance of the real weapon meshes or GPU trails. Native API compilation with the configured Unity version, LateUpdate/render ordering, hit-frame appearance, transparent sorting and mobile performance require engine/device validation.
