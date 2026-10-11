# Blender rigid actor modules

Original scripted geometry made in Blender 4.3.2, with no Meshy, paid assets, external artwork or textures. The modules replace selected **rigid mesh pieces**, not complete characters or animation clips. Production uses the existing factories and existing joint transforms, source materials, equipment dimensions and palette. The incomplete Vanguard FBX pilot remains separately default-off.

## Covered construction calls

| Module | Exact production pieces | Coverage |
|---|---|---|
| Cuirass | Breastplate capsule | Vanguard, goblin and guardian base torso |
| ClothTorso | Breastplate, explicitly selected in BuildHero | Arcanist, Ranger, Summoner base torso |
| BarkTorso | Breastplate, explicitly selected in Companion | Treant torso |
| CuirassPlate | Raised breastplate | Vanguard equipped armor across existing tiers |
| Pauldron | Pauldrons, Shoulder shell, Single leather shoulder | Existing shared base shoulders, Vanguard equipment, Ranger |
| ClothShoulder | Tailored shoulder mantle | Arcanist/Ranger equipment |
| Helmet | Helmet, Leather Cap | Vanguard and goblin |
| Boot | Boot, Grounded claw | Existing humanoid boots and large-boss claws |
| WolfHead / WolfTorso / Paw | Wolf head / Spirit wolf torso / Paw | Wolf companion |
| SpiritCore | Star spirit, Spirit Core, Exposed star heart, Arcane Crystal, Focus crystal | Companion, wisp enemy, large boss, base/equipped caster staff |
| Slime | Slime Body | Slime enemy |
| Hammer | Great Hammer | Guardian and guardian boss |
| Totem | Bound spirit totem | Summoner base outfit |
| SwordGuard | Crossguard, Swept guard | Base and equipped sword |
| BowLimb | Bow Limb, Layered bow limb | Base and equipped bow |
| StaffCrown | Staff Crystal Crown, Crystal prong | Base and equipped caster staff |
| BossHousing / BossPlate | Faceted engine housing / Curved shell plate | Large expedition boss chassis and animated core armor |

Shared base parts naturally cover multiple actors. This does not claim a unique whole-character redesign per actor. Existing sword blades, bows' attachment/string endpoints, staff shafts, eye/face feedback, cloth simulation, class costumes, tier/upgrade/fashion insignia, enemy weapons other than guardian hammer, foliage, boss rings/power rails and all other unmapped pieces remain their existing authored/procedural geometry. No textures/materials/renderer count added. No collider, reach, stats, timing, animation events, root motion, navigation or camera rules changed. Actor geometry does not globally replace ProceduralVisuals, so world and runtime hazard meshes are unaffected.

## Runtime and rollback

`AuthoredActorMeshes.Apply` runs after the original mesh constructor; exact name AND primitive type select a module. Missing, truncated, oversized, non-finite, invalid-normal/index payloads leave the original mesh. The versioned `EFM1` binary contains Blender-produced Unity-axis vertices, flat normals, UVs and indices. It avoids engine-dependent FBX hierarchy/axis extraction. Mesh topology is authored offline, not rebuilt from a runtime shape recipe. Cache owns these shared meshes and releases them on subsystem reset; actor/world disposal never owns them.

Set `AuthoredActorMeshes.Enabled=false` before rebuilding actors to use the original geometry. Existing actors must be recreated; the switch does not restore live instances. Startup defaults true. All other existing material and renderer behaviors remain on the same GameObjects, including recoil, fade, costume hide/show and equipment replacement. Stable UUID-derived `.meta` GUIDs are created only if missing.

Every module retains the original primitive origin and full axis-aligned envelope: cube/sphere 1×1×1, capsule/cylinder 1×2×1, before existing local scaling. Thus the existing body/gear/joint dimensions remain authoritative. This preserves bounds, not identical visual surface shape.

## Source and resource contract

`Emberfall-ActorModules.blend` is the editable original module scene. The original geometry and export definitions live in `build_actor_modules.py`; rebuilding writes the Unity-axis binary resources and preserves existing resource `.meta` files. These rigid meshes use the original material, renderer and feedback budgets; no separate LOD is added.

## Reproduce

```sh
blender -b --factory-startup --threads 4 --python ArtSource/ActorModules/build_actor_modules.py
python3 Tests/ActorModulesProductionTests.py /path/to/dotnet
python3 ArtSource/ActorModules/export_constructions.py "$PWD" /tmp/actor-constructions /path/to/dotnet
python3 ArtSource/ActorModules/validate_constructions.py /tmp/actor-constructions/geometry.json /tmp/actor-constructions/checks
```

Actual loader tests cover malformed data, normal/index/vertex budgets, bounds, exact mapping exclusions, cached reuse and rollback. Existing equipment/fashion composition suites explicitly test the original fallback boundary; they do not silently claim loaded-module coverage. The separate construction exporter executes the real loader. Three-platform API compilation and managed checks are not Unity acceptance.


## Wolf module contract

WolfHead now has a longitudinal cheek/skull shape; WolfMuzzle tapers forward from the original bite hinge; WolfEar has a pointed tapered silhouette. WolfTorso separates the broad chest and haunch from the narrow belly. Paw is the thicker foreleg with a distinct ankle/toe; WolfHindLeg carries the larger haunch. WolfTail has a fuller taper. The wolf-only factory scales the legs/ears/tail and seats the eye primitives into the reshaped skull. No joint origin, animation branch, navigation body, damage range, attack timing or gameplay parameter changed. Generic Ear/Muzzle/Tail/Paw names are **not** mapped globally: explicit wolf meshModule keys select them. Wolf-specific dispatch leaves other actors' module selections unchanged.

Existing WolfHead/WolfTorso/Paw GUIDs are unchanged. New resources use the same deterministic GUID namespace. Missing/corrupt resources or `AuthoredActorMeshes.Enabled=false` retain procedural geometry at the current cosmetic socket scales.

Reproduce geometry and checks:

```
blender -b --factory-startup --python ArtSource/ActorModules/build_actor_modules.py
python3 ArtSource/ActorModules/export_wolf_motion.py "$PWD" /tmp/emberfall-wolf /path/to/dotnet
python3 ArtSource/ActorModules/validate_constructions.py /tmp/emberfall-wolf/geometry.json /tmp/emberfall-wolf/checks
python3 Tests/ActorModulesProductionTests.py /path/to/dotnet
```

The maintained geometry checks cover leg/body and eye/head attachment contacts. The wolf motion exporter exercises the existing quadruped branch, limb resources, bite parent, jaw motion, finite vertices and navigation-owner isolation. These managed geometry checks do not replace native Unity import, animation, collision, rendering or device acceptance.
