# F1: finite class and companion silhouette closure

Scope is Arcanist, Ranger, Summoner, Spirit companion and Treant companion. No new identity, Animator, collider, damage range, timing or movement system. No third-party inputs, Meshy, purchases, new textures or materials. Blender 4.3.2 sources are original scripted geometry.

## Class and companion contracts

| Identity | Retained after inspection | Targeted authored change |
|---|---|---|
| Arcanist | The existing bent pointed hat, brim, high collar, staff sockets and dynamic TailoredCloth already give a readable mage identity. | Front/embroidered robe strips gain a folded section and pointed/scalloped hem, preserving separate spine attachment and existing equipment logic. |
| Ranger | Existing hood feather, bow wrist/limbs/string, chest wrap, harness and quiver placement remain. Chest-wrap overlap with the far-side bow is not newly diagnosed as a defect. | Open-faced hood shell; hollow, raised quiver lip; feathered spare arrows in the original three sockets. Actual draw pose is executed by production AimArm/BowDraw. |
| Summoner | Existing branch layout, moonstone, leaf mantle, short ritual outfit, wood totem, dynamic class cloak and tier adornments remain distinct from Arcanist. | Curved tapering antler/branch mesh replaces straight capsules. This is a targeted refinement, not a claim that a new whole character or new leaf-cloak simulation was made. |
| Spirit | Existing faceted core, permanent contract band/seal and rank vanes remain. | Open moon crown and paired triple-lobe wings. Both wings now use CompanionRigidParent, so they follow actual preparation/body rotation without soft core scaling. |
| Treant | Existing BarkTorso, shoulder origins/shells, eyes, belt, contract knot and rank branches remain. No shoulder pivot is moved. | Four-lobed faceted foliage per existing crown socket; bark face, tapered bark limbs/hands and root-like feet in the original articulation. |

## Actual integration and rollback

`CombatModel.Part` calls `ActorSilhouetteF1.Apply` after the existing AuthoredActorMeshes loader, retaining the original MeshFilter, Renderer, joint, scale and palette. The F1 dispatch names are exact. Generic Head/Sleeve/Trouser/Glove/Boot keys apply **only** when the existing `treantCompanion` flag is true; other characters, enemies, scenery and weapons do not receive those replacements. The same named robe strips in equipped armor are upgraded when their existing builder creates them. `ApplyEquipment` and `ApplyFashion` keep their existing ownership and visibility behavior.

There are twelve shared cached meshes in `Assets/Resources/ActorSilhouettes/F1`, with a 512-triangle per-piece cap. No textures, materials, renderers or Animators are added. Mobile low quality uses the same rigid meshes.

Missing/corrupt data and `ActorSilhouetteF1.Enabled=false` keep the previous procedural/authored filter mesh. The Spirit crown/wing dimensions are carried by the mesh data; original primitive construction scales remain unchanged for fallback. Existing GUIDs are unchanged; new files have deterministic GUIDs. The wing's rigid-parent correction is retained in fallback. Meshes are destroyed only by subsystem cache reset, not by individual actor disposal.

## Maintained checks

`ActorSilhouetteF1ProductionTests.py` covers the actual resources and decoder, cache reuse, role isolation, explicit/missing/malformed fallback, module use in production construction, attachment contacts and compiled negative controls for generic-role replacement and soft-core wing parenting. The construction exporter uses managed Unity transform/resource boundaries; it does not replace native import, rendering, cloth simulation or device acceptance.

## Reproduce

```sh
blender -b -t 2 --factory-startup --python ArtSource/ActorSilhouettes/F1/build.py
python3 Tests/ActorSilhouetteF1ProductionTests.py /path/to/dotnet
python3 ArtSource/ActorSilhouettes/F1/export.py /tmp/f1-production /path/to/dotnet
```

The editable `ActorSilhouettes-F1.blend` contains all twelve source meshes with stable names; source-only contact placement does not affect runtime export.
