# Tactical body fittings

Original Emberfall assets made with Blender 4.3.2; no external model, image, font or paid service. `TacticalAttachments.blend` remains editable; mesh objects retain their names. Review torso spheres, lights and camera are source-only.

SupplierBackpack has twin faceted energy reservoirs, rear brace and over-shoulder feeds. HuntBadge is a forward split-blade badge with a target stone. SupportMantle has separate lateral protective shoulder fins. The fittings use three shared opaque Standard materials and no textures. Each live enemy adds at most three MeshRenderers; low quality retains the same compact identity geometry, with no particles, transparency or extra lights.

`TacticalEnemyVisual.Refresh` reads the existing supplier/hunt/support predicates; only identity presentation is replaced. Contested ground marking is unchanged. Per-body mesh bounds and parent transforms adapt fittings to slime, wisp, humanoid, guardian and boss scaling/animation. Rear/center-front/lateral sockets keep simultaneous states separate. Each loaded fitting suppresses its old line icon. Missing/corrupt resources, unsupported shader, unknown body and the explicit Enabled rollback retain the old individual marker. Loss of support removes the fitting immediately; the original 0.24-second severed line tail remains only in fallback mode. No support range, damage multiplier, control duration or other gameplay value changes.

Shared mesh/material cache is destroyed only at subsystem reset; enemy lifecycle destroys owned fitting objects. Refresh makes no mesh/material/GameObject allocations. Disable, death, epoch mismatch and replacement player hide all fittings. No renderer lists are cached in the owning model by this adapter.

The guardian badge sits in front of the actual chest plate and raised ember crystal, using outer-shell bounds converted into the animated body's local frame. It retains the body parent and transform scale, so ordinary/large guardian scaling remains inherited. Slime and wisp use a smaller lower-front seal to preserve eye/face identity. Humanoid support fins are raised and spread over shoulder armour. The supplier socket is unchanged. Each active role owns one renderer.

The HuntBadge generator applies rotation/scale after joining and exports world-space positions plus inverse-transpose normals. The numeric regression checks decoded first-blade center `(-.14,.03,.60)` and collar center `(0,.27,.59)` within 1e-6. Resource .meta GUIDs remain stable.

## Rebuild and maintained checks

From repository root:

```sh
blender -b --factory-startup --python ArtSource/TacticalAttachments/build.py
python3 Tests/TacticalAttachmentsProductionTests.py /path/to/dotnet
python3 Tests/TacticalEnemyAssemblyTests.py /path/to/dotnet
```

The reusable construction exporter can write production enemy/fitting geometry to a local output directory:

```sh
python3 ArtSource/TacticalAttachments/export_enemies.py "$PWD" /tmp/tactical-enemies /path/to/dotnet
```

The live-state/component checks exercise `TacticalEnemyVisual`, `TacticalAttachmentArt`, the shared decoder and real binary buffers. The assembly checks exercise `CombatModel.Enemy` plus `TacticalAttachmentArt.Create/Set` for all role masks across slime, wisp, goblin, guardian and guardian boss. They check renderer counts, armour/eye clearance, socket placement and hiding, with mutation controls for buried fittings.

Managed engine substitutes do not establish Standard shader build inclusion, GPU appearance, device FPS, Unity batching or clipping during real animation. Those require engine/device review.
