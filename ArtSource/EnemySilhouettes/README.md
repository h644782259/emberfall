# F2 — enemy and encounter silhouette modules

Nine original Blender rigid modules replace existing visible meshes at the end of the actual `CombatModel.Enemy` and `LargeExpeditionBoss.CreateAnchors` factories. One assembled rig owns every action; no clip swaps bodies. Existing node names, hierarchy, local TRS, palette materials, hit shapes, control clocks, anchor count/mask/level/radius and encounter phases remain unchanged. No purchases or external source art.

## Runtime scope

- Goblin: pointed folded ears, asymmetric leather cap/rim and curved beveled knife. Original face, limbs and articulated animation remain.
- Guardian and GuardianBoss: beveled chest, layered stone shoulders and open toothed crown. Original hammer, exposed crown crystal, boss proportions, tactical sockets and knockdown/death ownership remain.
- Slime and Wisp: original factories, geometry, palettes and animated forms remain.
- Large astrolabe: existing `LargeBossRig.Build` body remains; only anchor plinth, crystal and cage claws use these modules. `CreateAnchors` retains placement and state ownership.

## Source and resource contract

`EnemySilhouettes.blend` is the editable source scene outside Unity Assets. `build.py` contains the original geometry and Unity-axis binary export definitions. The nine reusable modules use the existing shared surface palette, with a 512-triangle cap per module and no new textures or runtime materials. GUIDs are deterministic on first creation and preserved on rebuild. Runtime import uses the strict EFM1 decoder.

Low quality uses the same bounded meshes, with no separate geometry tier or added lights, materials or textures.

## Fallback and lifecycle

`EnemySilhouetteArt.Enabled=false` before construction retains the complete prior factory presentation. Missing, malformed or over-envelope individual modules retain their original mesh; failures cache once. Envelope and ≤512-triangle checks supplement the strict decoder. Cache shares meshes and resets at subsystem registration. This is a startup/construction switch, not a live toggle that restores already-replaced meshes. Old mesh ownership is unchanged; no gameplay component or collider is replaced.

## Reproduce and validate

Run from repository root (Blender 4.3.2, .NET 8):

```sh
blender -b -t 2 --factory-startup --python ArtSource/EnemySilhouettes/build.py
python3 ArtSource/EnemySilhouettes/export_validate.py . /tmp/f2-production /path/to/dotnet
# Tests/EnemySilhouetteProductionTests.py is the discoverable equivalent using a temporary directory.
DOTNET=/path/to/dotnet python3 Tests/EnemySilhouetteAnimationTests.py --output /tmp/f2-animation.json
DOTNET=/path/to/dotnet python3 Tests/EnemyKnockdownGeometryTests.py --output /tmp/f2-down.json
DOTNET=/path/to/dotnet python3 Tests/EnemyKnockdownProductionTests.py
```

These checks cover the real decoder/factory/cache/fallback/anchor paths, articulated animation, knockdown geometry and death-pose ownership using explicit managed boundaries. Native Unity shading, camera occlusion, particles, runtime batching and device behavior require separate engine acceptance. Generated check outputs belong in temporary or ignored local directories.
