# Reusable spell surfaces

Nine original Blender-authored surface refinements replace the base mesh selection in `FilledSkillVfx.EnsureAssets`. Existing placement, anchored cover clipping, animation curves, motion footprint reservations, shader, priority leases, object counts, reduced/mobile caps, damage, timing and growth remain unchanged. Each missing/malformed buffer individually falls back to its existing `FilledVfxRecipes` shape.

These are refinements to existing identity silhouettes. The Arcane lattice remains procedural. Danger boundaries, weapon trails, live targeting, dynamic beam endpoints, field scheduling and physics remain runtime systems.

| Shape | Surface identity |
|---|---|
| Crescent | Closed diamond section and UV hot outer edge |
| Crystal | Faceted shoulder distinguishes the ice spear |
| Flame | Curved flame contour and normalized height UV |
| Sword | Tapered blade shoulder |
| Lightning | Tapered branch endpoints |
| ArcaneShard | Tapered broken strut end |
| Rupture | Tapered fault tips |
| Arrow | Filled diamond head |
| Vine | Tapered branches and stems |

Runtime buffers add no textures and use the same shared material. Per-effect renderer budgets remain 14 desktop / 10 mobile / 7 reduced. Source buffers stay within the original bounds with floating-point tolerance. Anchored clipping may subdivide faces under its existing independent budget.

UV semantics preserve the shader's identity cues: Crescent outer edge 1 / inner edge 0 / ridges .6, Flame normalized height, Crystal brightest at tip (shoulder .55), other volumes projected X + clamped height mapping. `Reusable-Spell-Volumes.blend` contains editable meshes in Blender Z-up; buffers explicitly export Unity Y-up. Strict shared `AuthoredActorMeshes.Decode` owns decoding; FilledSkillVfx cache owns and destroys its decoded meshes. Deterministic metas preserve resource GUIDs.

## Dedicated Ice / Fire primaries

Two dedicated primary resources shape actual `Impact` composition: IcePrimary is a shouldered central spear with four basal facets; FirePrimary contains three split tapered tongues. Both meshes stay inside the previous primary's base bounding envelope.

The release shows contact immediately. Visual-only motion 13 rises from 45% height to full height in .09 seconds, holds until .42 seconds, then contracts to 62%; motion 14 rises from 55% to full height in .07 seconds, holds until .23 seconds, then contracts to 48% height and 76% radial scale. Radial contraction preserves the anchored certified rays. No damage callback or new impact flash accompanies these visual phases; lifetime and impact/area rules remain unchanged. Dedicated-resource failure falls back to the previous mesh and motion.

For these authored Ice/Fire effects only, repeated ornaments shrink to 60% scale, use 55% opacity and fade between .42 and .62 seconds. Reservations remain conservative, all tier counts remain unchanged, and the primary and contact are first allocated. This keeps identity visible on the reduced tier. Other types and legacy fallback are unaffected.

## Rebuild and maintained checks

From repository root:

```sh
blender --background --python ArtSource/BlenderSpellBases/build.py
python3 ArtSource/BlenderSpellBases/validate.py
python3 Tests/AuthoredSpellBasesProductionTests.py /path/to/dotnet
python3 Tests/AuthoredSpellIntegrationTests.py /path/to/dotnet
```

The maintained loader check executes the production adapter and decoder with real data and missing/malformed fallback. Legacy allocation suites stub the resource boundary to exercise procedural fallback.

`Tests/AuthoredSpellIntegrationTests.py` exercises real resources → shared decoder → `FilledSkillVfx` → `AnchoredImpactMesh` / `CombatVisualLease` with managed scene/Unity API doubles. It covers six effect kinds, desktop/mobile/reduced tiers, wall/open contexts, animated clipped vertices, missing/corrupt fallback, lease saturation, pause, epoch/death/session/end/expiry cleanup, owned-mesh retirement and source-cache reset ownership. Negative controls check resource loading, reduced budgets and clipping. Managed checks do not establish Unity shader, transparency or device acceptance.
