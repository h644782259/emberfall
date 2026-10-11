# Blender scenery

Finite coverage: existing supply crate, wayfarer tent and ember campfire exports activated independently of the experimental hero; hollow ceramic pot, fractured rubble and open-canopy tree integrated at the existing WorldBuilder and DestructiblePropFactory call sites. Procedural roofs, dynamic room layouts, danger markers and water remain under their existing systems.

Original Emberfall geometry authored by `build_scenery.py` using Blender 4.3.2 primitives and explicit geometry; no downloaded assets, Meshy, purchase, external textures or third-party art. The three reused pilot props retain their existing `ArtSource/BlenderPilot` provenance and editable source; they are not regenerated or copied.

All tiers use the new bounded opaque meshes (ceiling 1,024 triangles each), without alpha foliage, particles, per-instance textures or high-tier mesh duplicates. Materials use the world's shared material cache and existing visual-surface settings. Each new asset is one mesh renderer with one or two material slots. Reused tent/crate/fire use the existing shared pilot atlas; no duplicate atlas is added.

`Emberfall-Scenery.blend` contains editable meshes plus clearly named review camera/stage; FBX exports are created before review placement. Deterministic asset paths and checked-in .meta GUIDs remain stable on rebuild. FBX axes are -Z forward/Y up, with Blender source in meters/Z up. Rubble embeds about 6 cm into the ground, preventing a floating base; tree base is within 5 mm of ground.

## Rebuild and maintained checks

From repository root:

```sh
blender -b -t 4 --python ArtSource/BlenderScenery/build_scenery.py
blender -b -t 2 --python ArtSource/BlenderScenery/validate_exports.py
DOTNET=/path/to/dotnet python3 Tests/BlenderSceneryProductionTests.py
```

The only placement changes are visual children: destructible radius, level, health/recovery initialization, path blocking, destruction ownership and original model Transform remain unchanged. Tree position/size/seed rotation and the original .22 m navigation trunk registration remain authoritative. The whole imported tree is registered as one camera-occlusion hierarchy. Campfire retains original light parameters and stone navigation circles; its existing folded flame mesh receives only a small procedural pulsing inner core. The authored ring differs from individual rocks, while legacy circle coordinates/radii remain authoritative. No damage range, timing or growth values are edited.

`BlenderSceneryArt.Enabled=false` before world creation restores procedural paths. Missing model, missing/unsupported atlas, missing geometry, unexpected new-material names or colliders in model resources fail closed to the old art. The hero pilot flag is not modified. Existing loaded worlds are not rebuilt automatically by toggling the flag.

The export validator checks FBX round-trip bounds and material slots. Managed production checks cover loader success/failure, fallback geometry, occlusion registration and scene presentation using API doubles. They do not establish Unity importer, GPU, device or performance acceptance. Material remapping, imported scale/orientation and in-game composition require engine review; procedural fallback remains available.
