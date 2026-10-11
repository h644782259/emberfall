# Vanguard authored skill surfaces

Original Emberfall geometry with no external assets, textures, purchases or generated gameplay rules. Editable source: `Vanguard-Skills.blend`; generator: `build.py`.

`BlenderSkillVfx.TryPlay` is integrated at the existing Vanguard slot 0/1 release. Melee damage, range, cone, status durations, rank followups and growth values are unchanged. Whirlwind uses three closed-volume amber blades and two separately authored narrow hot cores; ground shock keeps the immediate contact ring and weapon ribbon, then adds a short crest/fault/chip wake. A 1.15-second tail is visual only and implies no later impact. The effect remains at the cast origin.

Only missing/invalid assets, unsupported shader, or an uncertified full rotating footprint use the original presentation. Near cover the existing path is retained instead of projecting an authored effect through a wall. Budget rejection suppresses the decoration without resubmitting a lower-priority fallback. Owner death, epoch change, session replacement/end retire it; blocked input and zero scaled delta pause it.

Runtime geometry is little-endian `.bytes`, decoded by the shared strict `AuthoredActorMeshes.Decode` loader. Blender Z-up is explicitly converted to Unity Y-up `(x,z,-y)` for vertices and normals; no FBX/OBJ import transform is required. Runtime GUIDs are deterministic and remain stable across rebuilds.

The four runtime buffers use no textures and share the existing FilledSpell shader/material. Per-cast caps are 12 desktop, 8 mobile and 5 reduced renderers. Global CombatVisualLease limits remain authoritative.

Danger/target boundaries, dynamic cover clipping, actual weapon endpoint ribbons, live beam endpoints, collision/damage and area scheduling remain runtime systems.

## Rebuild and maintained checks

From repository root:

```sh
blender --background --python ArtSource/BlenderVfx/build.py
python3 ArtSource/BlenderVfx/validate.py
DOTNET=/path/to/dotnet python3 Tests/BlenderSkillVfxProductionTests.py
```

The maintained production-component checks execute the decoder, VFX class and shared lease using managed engine substitutes and real resource buffers. Negative controls check age-zero initialization, epoch cleanup, footprint containment and zero-normal rejection. These checks do not execute Unity or the caller damage pipeline; shader transparency, device performance and in-game composition require engine/device review.
