# Authored projectiles and trap preparation

Original Emberfall meshes; no external assets, paid services or textures. `build.py` authors editable `Projectiles.blend` and exports Unity-axis EFM1 bytes through the existing strict `AuthoredActorMeshes.Decode` contract. GUIDs are deterministic and retained when rebuilding.

ContractBolt is one closed spirit seed. Four elliptical rings and pointed ends create a filled body with three shallow longitudinal grooves and slight asymmetric swelling; smooth vertex normals provide a continuous surface for the existing material. Local bounds remain ±.49 X, ±.14 Y and ±.474 Z. The basic-attack factory supplies its visible world volume.

Actual integration is `CombatProjectile.Friendly/BasicShot/Hostile → Make → AuthoredProjectileMeshes.Load`. Only the existing visual child's mesh changes. The root, trail, source bridge, material ownership, collision radius, trajectory, target, volley, damage, event timing and lifetime are unchanged. An arrow uses its explicit flag; companionSource or Summoner identifies a contract bolt; other friendly non-arrows use a decorative caster shape; Hostile explicitly selects the enemy shape. Colors never choose geometry or imply a real damage element. Positive local Y is arrowhead-forward under the existing +90° X root rotation.

`CombatArea.Spawn` loads MeteorRock with the existing WeatheredRock fallback. The single kernel has recessed channels and facets, without a new flame renderer. Original 9→.5 quadratic descent, 120/70 rotation and disappearance at startup are intact.

The Ranger Neutral/statusSkill1/startup>0 preparation gets TrapCore. The rank2 follow-up does not create a second fake trap. This optional child has a Decoration lease using existing desktop32/mobile20/reduced12 caps. Higher priority contacts can evict it without touching the gameplay area. AnchoredImpactMesh clips the core against actual cover; a result exceeding128 triangles omits the optional core and preserves the original marker. The area removes it at its existing age>=delay boundary and owns destruction on room/death retirement. The component owns its clipped mesh/material; cache owns source meshes. No additional gameplay update loop.

Arrow bounds fit the original capsule ±.5XZ/±1Y; bolts fit the original sphere ±.5XYZ. Low tier uses the same primary meshes and original trail time .08 vs .16; no increased trail settings or renderer count for projectiles.

Individual missing/malformed resources use their primitive/rock fallback; missing trap leaves the original marker/charge feedback. Internal Enabled=false allows whole adapter fallback for diagnosis. Subsystem reset destroys cached meshes and resets failed loads; projectile retirement destroys only its original materials, not shared mesh.

## Rebuild and maintained checks

From repository root:

```sh
blender -b -t 2 --python ArtSource/BlenderProjectiles/build.py
python3 Tests/AuthoredProjectileProductionTests.py /path/to/dotnet
```

To export only the spirit seed, preserving all other resource bytes:

```sh
blender -b -t 2 --python ArtSource/BlenderProjectiles/build.py -- --only ContractBolt
```

The selective export leaves the combined `Projectiles.blend` unchanged; the full build regenerates that editable scene from the current source.

The maintained runner compiles the production projectile factory, muzzle/flight alignment and retirement methods with real resource bytes, decoder, procedural recipes and adapter. Unity/resource APIs, renderer and muzzle bridge use managed doubles. It checks simulation fields and root transforms, target/volley, tier trail settings, cache/material ownership and missing/corrupt resource fallback. Area checks exercise the production cosmetic spawn and age/movement blocks for meteor descent, first-only trap, clipping, leases and cleanup. These checks do not establish Unity rendering, native physics, platform package size or device frame time.
